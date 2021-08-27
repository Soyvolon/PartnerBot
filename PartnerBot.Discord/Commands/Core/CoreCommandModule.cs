using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using DSharpPlus.Exceptions;
using DSharpPlus.Interactivity;
using DSharpPlus.Interactivity.Extensions;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Services;
using PartnerBot.Core.Utils;

namespace PartnerBot.Discord.Commands.Core
{
    public class CoreCommandModule : CommandModule
    {
        // Items with three outputs (object, string?, bool) follow: (output, error message, fatal error). Commands should immedietly stop running if a
        // fatal error is returned.
        
        // TODO: Embed field adding needs buttons.

        protected DiscordEmbed SetupBase { get; set; } = new DiscordEmbedBuilder()
        {
            Title = "Partner Bot Setup - Main",
            Author = new()
            {
                Name = "Partner Bot",
            },
            Footer = new()
            {
                Text = "PB Setup - Type exit to quit the setup."
            },
            Color = Color_PartnerBotMagenta
        };

        protected async Task<(ComponentInteractionCreateEventArgs, bool)> GetButtonPressAsync(InteractivityExtension interact, DiscordMessage baseMessage)
        {
            var res = await interact.WaitForButtonAsync(baseMessage);

            if(res.TimedOut)
            {
                await InteractTimeout("Setup canceled, interaction timed out.");
                return (res.Result, false);
            }

            return (res.Result, true);
        }

        protected async Task<(InteractivityResult<DiscordMessage>, bool)> GetFollowupMessageAsync(InteractivityExtension interact)
        {
            InteractivityResult<DiscordMessage> res = await interact.WaitForMessageAsync(x => x.Author.Id == this.Context.Member.Id
                    && x.ChannelId == this.Context.Channel.Id);

            if (res.TimedOut)
            {
                await InteractTimeout("Setup canceled, interaction timed out.");
                return (res, false);
            }

            try
            {
                await res.Result.DeleteAsync();
            }
            catch
            {
                // Well they are going to have lots of spam.
            }

            return (res, true);
        }

        protected async Task<((DiscordChannel, DiscordWebhook, string)?, string?, bool)> GetNewPartnerChannelAsync(Partner partner,
            ComponentInteractionCreateEventArgs interaction)
        {
            
            InteractivityExtension? interact = this.Context.Client.GetInteractivity();
            DiscordColor color = DiscordColor.Purple;

            var statusEmbed = new DiscordEmbedBuilder()
                    .WithTitle("Partner Bot Setup - Channel")
                    .WithDescription("Please select a channel that you would like to receive partner messages in. This channel will" +
                    " receive any messages from other servers when your advertisement is sent out.\n\n" +
                    "The channel requires all overwrites in that channel have the following two permissions:" +
                    " `View Channel` and `Read Message History`")
                    .WithColor(color);
            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed);

            var statusMessage = await builder.SendAsync(interaction.Channel);
            try
            {
                DiscordChannel? c = null;
                bool valid = false;
                do
                {
                    (InteractivityResult<DiscordMessage>, bool) folloup = await GetFollowupMessageAsync(interact);

                    if (!folloup.Item2) return (null, null, true);

                    InteractivityResult<DiscordMessage> res = folloup.Item1;

                    if (res.Result.Content.Trim().ToLower().Equals("exit"))
                    {
                        await RespondError("Setup cancled");
                        return (null, null, true);
                    }
                    else if (res.Result.MentionedChannels.Count <= 0)
                    {
                        await statusMessage.ModifyAsync(statusEmbed
                            .WithDescription("No channel was selected. Please mention a channel or type `exit` to quit.")
                            .WithColor(DiscordColor.DarkRed)
                            .Build());
                    }
                    else
                    {
                        DiscordChannel? mentioned = res.Result.MentionedChannels[0];

                        if (GuildVerificationService.VerifyChannel(mentioned))
                        {
                            valid = true;
                            c = mentioned;
                        }
                        else
                        {
                            string desc = "**Invalid Channel Setup.**\n" +
                                $"Some overwrites are missing the `View Channel` or `Read Message History` for {mentioned.Mention}";
                            (List<string>, List<DiscordOverwrite>) invalidRes;
                            try
                            {
                                invalidRes = await GetInvalidChannelSetupDataString(mentioned);
                            }
                            catch (UnauthorizedException)
                            {
                                await statusMessage.ModifyAsync(statusEmbed
                                    .WithDescription("Partner Bot does not have access to the selected channel. Please select a channel" +
                                    " Partner Bot does have access to.")
                                    .WithColor(DiscordColor.DarkRed)
                                    .Build());
                                continue;
                            }

                            List<string>? data = invalidRes.Item1;
                            List<DiscordOverwrite>? invalid = invalidRes.Item2;

                            desc += $"**Would you like Partner Bot to fix this channel for you? If so, type `yes`." +
                                $" Otherwise, type `no` to return to channel selection.**\n\n\n" +
                                $"{string.Join("\n\n", data)}";

                            statusMessage = await statusMessage.ModifyAsync(new DiscordMessageBuilder()
                                .WithEmbed(statusEmbed
                                    .WithDescription(desc)
                                    .WithColor(DiscordColor.Red))
                                .AddComponents(
                                    new DiscordButtonComponent(ButtonStyle.Success, "yes", "Fix Channel"),
                                    new DiscordButtonComponent(ButtonStyle.Secondary, "no", "Ignore Channel"),
                                    new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Exit Without Saving")
                                ));

                            var btnFolowup = await GetButtonPressAsync(interact, statusMessage);

                            if (!btnFolowup.Item2) return (null, null, true);

                            var btnRes = btnFolowup.Item1;

                            if (btnRes.Id.Equals("exit"))
                            {
                                await RespondError("Setup cancled");
                                return (null, null, true);
                            }
                            else if (btnRes.Equals("yes"))
                            {
                                (bool, string?) fix = await ConfigurePartnerChannelPermissions(invalid);

                                if (fix.Item1)
                                {
                                    valid = true;
                                    c = mentioned;
                                }
                                else
                                {
                                    await btnRes.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                        new DiscordInteractionResponseBuilder().AddEmbed(statusEmbed
                                        .WithDescription($"Partner Bot was unable to automatically setup the channel:\n" +
                                        $"{fix.Item2}\n\n" +
                                        $"Please fix the channel manually or select a new channel. Mention a channel to continue.")
                                        .WithColor(DiscordColor.DarkRed)));
                                }
                            }
                            else
                            {
                                await btnRes.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                    new DiscordInteractionResponseBuilder().AddEmbed(statusEmbed
                                    .WithDescription("Please select a channel that you would like to receive partner messages in. This channel will" +
                                    " receive any messages from other servers when your advertisement is sent out.\n\n" +
                                    "The channel requires all overwrites in that channel have the following two permissions:" +
                                    " `View Channel` and `Read Message History`")
                                    .WithColor(color)));
                            }
                        }
                    }
                } while (!valid);

                if (c is null) return (null, "Critical Error: Channel failed to propagate", false);

                DiscordWebhook hook;
                if (partner.WebhookId != 0)
                {
                    try
                    {
                        hook = await this.Context.Client.GetWebhookWithTokenAsync(partner.WebhookId, partner.WebhookToken);
                    }
                    catch
                    {
                        hook = await c.CreateWebhookAsync("Partner Bot Message Sender", reason: "Partner Bot Sender Webhook Update");
                    }
                }
                else
                {
                    hook = await c.CreateWebhookAsync("Partner Bot Message Sender", reason: "Partner Bot Sender Webhook Update");
                }

                if (hook.ChannelId != c.Id)
                {
                    await hook.ModifyAsync("Partner Bot Message Sender", channelId: c.Id);
                }

                string invite;
                if (string.IsNullOrWhiteSpace(partner.Invite))
                {
                    DiscordInvite? fullinvite = await c.CreateInviteAsync(0, 0, false, false, "Partner Bot Invite");
                    invite = fullinvite.Code;
                }
                else
                {
                    invite = partner.Invite;
                }

                return ((c, hook, invite), null, false);
            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }

        public static async Task<(List<string>, List<DiscordOverwrite>)> GetInvalidChannelSetupDataString(DiscordChannel channel)
        {
            List<string> data = new();
            List<DiscordOverwrite> invalid = new();

            foreach (DiscordOverwrite? o in channel.PermissionOverwrites)
            {
                bool access = o.Allowed.HasPermission(Permissions.AccessChannels);
                bool read = o.Allowed.HasPermission(Permissions.ReadMessageHistory);

                if (access && read)
                    continue;

                invalid.Add(o);

                string msg = "";

                if (o.Type == OverwriteType.Member)
                {
                    DiscordMember? m = await o.GetMemberAsync();

                    msg += $"**Invalid Member: {m.Mention}**\n";
                }
                else
                {
                    DiscordRole? r = await o.GetRoleAsync();

                    msg += $"**Invalid Role: {r.Mention}**\n";
                }

                msg += $"{(access ? ":white_check_mark:" : ":x:")} `View Channel`\n" +
                    $"{(read ? ":white_check_mark:" : ":x:")} `Read Message History`";

                data.Add(msg);
            }

            return (data, invalid);
        }

        protected async Task<(bool, string?)> ConfigurePartnerChannelPermissions(List<DiscordOverwrite> invalid)
        {
            try
            {
                foreach (DiscordOverwrite? o in invalid)
                {
                    await o.UpdateAsync(GuildVerificationService.RequiredPermissions, reason: "Partner Bot Auto Channel Setup");
                }
            }
            catch (UnauthorizedException)
            {
                return (false, "Partner bot does not have permission to modify some channel overwrites.");
            }
            catch (Exception ex)
            {
                return (false, $"Some other error occurred: {ex.Message}");
            }

            return (true, null);
        }

        // TODO: Check for message size limits that leave space for the invite link.
        protected async Task<(string?, string?, bool)> GetNewMessage(Partner p, ComponentInteractionCreateEventArgs interaction,
            int linksReset)
        {
            
            InteractivityExtension? interact = this.Context.Client.GetInteractivity();

            var statusEmbed = new DiscordEmbedBuilder()
                .WithTitle("Partner Bot Setup - Message")
                .WithDescription("Welcome to the message setter. Please enter your new message.")
                .WithColor(DiscordColor.Aquamarine);

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed);

            var statusMessage = await builder.SendAsync(interaction.Channel);
            try
            {
                p.LinksUsed -= linksReset;

                bool first = true;
                DiscordMessage? pMessage = null;
                string? message = null;
                int linkCount = p.LinksUsed;
                do
                {
                    List<Task> taskPair = new();
                    taskPair.Add(GetFollowupMessageAsync(interact));
                    if(!first)
                        taskPair.Add(GetButtonPressAsync(interact, statusMessage));

                    var completedPoint = Task.WaitAny(taskPair.ToArray());

                    string msg = "";

                    if (completedPoint == 0)
                    {
                        var response = await (Task<(InteractivityResult<DiscordMessage>, bool)>)taskPair[0];

                        if (!response.Item2) return (null, null, true);

                        InteractivityResult<DiscordMessage> res = response.Item1;

                        msg = res.Result.Content;

                        string? trimmed = msg.ToLower().Trim();

                        if (trimmed.Equals("exit"))
                        {
                            await RespondError("Aborting...");
                            return (null, null, true);
                        }

                        if (msg.Length > 1900)
                        {
                            await statusMessage.ModifyAsync(statusEmbed
                                        .WithColor(DiscordColor.DarkRed)
                                        .WithDescription("A message cannot be longer than 1900 characters!")
                                        .Build());

                            continue;
                        }

                        if (pMessage is not null)
                            await pMessage.DeleteAsync();

                        linkCount = p.LinksUsed;

                        IReadOnlyList<string>? links = msg.GetUrls();

                        foreach (string? l in links)
                        {
                            if (linkCount >= p.DonorRank)
                            {
                                msg = msg.Remove(msg.IndexOf(l), l.Length);
                            }
                            else
                            {
                                if (l.ContainsDiscordUrl())
                                {
                                    msg = msg.Remove(msg.IndexOf(l), l.Length);
                                }
                                else
                                {
                                    linkCount++;
                                }
                            }
                        }
                    }
                    else if(!first)
                    {
                        var interRes = await (Task<(ComponentInteractionCreateEventArgs, bool)>)taskPair[1];

                        if (!interRes.Item2) return (null, null, true);

                        var res = interRes.Item1;

                        await res.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                        if (!first
                            && res.Id.Equals("save"))
                        {
                            if (!string.IsNullOrWhiteSpace(message))
                            {
                                if (message.Length > 1900)
                                {
                                    await statusMessage.ModifyAsync(statusEmbed
                                        .WithColor(DiscordColor.DarkRed)
                                        .WithDescription("A message cannot be longer than 1900 characters! Please input a valid message before saving.")
                                        .Build());

                                    continue;
                                }

                                break;
                            }
                            else
                            {
                                await statusMessage.ModifyAsync(statusEmbed
                                    .WithColor(DiscordColor.DarkRed)
                                    .WithDescription("A message cannot be empty! Please input a valid message before saving.")
                                    .Build());

                                continue;
                            }
                        }
                        else if (res.Id.Equals("exit"))
                        {
                            await RespondError("Aborting...");
                            return (null, null, true);
                        }
                    }

                    if (first)
                    {
                        builder.AddComponents(new DiscordComponent[]
                        {
                        new DiscordButtonComponent(ButtonStyle.Success, "save", "Save"),
                        new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Exit Without Saving")
                        });
                    }

                    statusMessage = await statusMessage.ModifyAsync(builder);

                    pMessage = await this.Context.RespondAsync(msg);
                    message = msg;

                    first = false;
                }
                while (true);

                if (pMessage is not null)
                    await pMessage.DeleteAsync();

                p.LinksUsed = linkCount;

                return (message, null, false);
            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }

        protected async Task<(Uri?, string?, bool)> GetNewPartnerBanner(ComponentInteractionCreateEventArgs interaction)
        {
            
            InteractivityExtension? interact = this.Context.Client.GetInteractivity();

            var statusEmbed = new DiscordEmbedBuilder()
                .WithTitle("Partner Bot Setup - Banner")
                .WithDescription("Welcome to the banner selector. Please upload a new image or input an image URL. The image URL must end in" +
                " an image extension such as `.png`, `.jpg`, or `.gif`")
                .WithColor(DiscordColor.HotPink);

            var buttons = new DiscordComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Success, "save", "Save"),
                new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Exit Withtout Saving")
            };

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed)
                .AddComponents(buttons);

            var statusMessage = await builder.SendAsync(interaction.Channel);

            Uri? bannerUrl = null;
            DiscordMessage? displayMsg = null;

            try
            {

                do
                {
                    List<Task> taskPair = new();
                    taskPair.Add(GetFollowupMessageAsync(interact));
                    taskPair.Add(GetButtonPressAsync(interact, statusMessage));

                    var completedPoint = Task.WaitAny(taskPair.ToArray());

                    if (completedPoint == 0)
                    {
                        var response = await (Task<(InteractivityResult<DiscordMessage>, bool)>)taskPair[0];

                        if (!response.Item2) return (null, null, true);

                        InteractivityResult<DiscordMessage> res = response.Item1;

                        if (displayMsg is not null)
                            await displayMsg.DeleteAsync();

                        if (res.Result.Attachments.Count > 0)
                        {
                            _ = Uri.TryCreate(res.Result.Attachments[0].Url, UriKind.Absolute, out bannerUrl);
                        }
                        else
                        {
                            IReadOnlyList<string>? links = res.Result.Content.GetUrls();

                            if (links.Count > 0)
                                _ = Uri.TryCreate(links[0], UriKind.Absolute, out bannerUrl);
                        }

                        if (bannerUrl is null)
                        {
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithDescription("No image was uploaded or an invalid link was provided. Please include the full link if you used a link.\n\n" +
                                "Type `exit` to quit, upload a new image, or input a new link.")
                                .WithColor(DiscordColor.DarkRed)
                                .Build());
                        }
                        else
                        {
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithDescription("The following image will be used as your banner image. Make sure it shows up properly in the embed, then type `save`." +
                                " If nothing shows up, make sure your link is correct or try uploading a new image.")
                                .WithColor(DiscordColor.HotPink)
                                .Build());

                            displayMsg = await this.Context.RespondAsync(new DiscordEmbedBuilder()
                                .WithImageUrl(bannerUrl));
                        }
                    }
                    else
                    {
                        var interRes = await (Task<(ComponentInteractionCreateEventArgs, bool)>)taskPair[1];

                        if (!interRes.Item2) return (null, null, true);

                        var res = interRes.Item1;

                        if (res.Id.Equals("exit"))
                        {
                            await RespondError("Aborting...");
                            await res.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                            return (null, null, true);
                        }
                        else if (res.Id.Equals("save"))
                        {
                            if (bannerUrl is not null)
                                break;
                            else
                            {
                                await res.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                    new DiscordInteractionResponseBuilder()
                                    .AddEmbed(statusEmbed
                                        .WithDescription("The banner URL can not be blank! Enter a URL to save.")
                                        .WithColor(DiscordColor.DarkRed))
                                    .AddComponents(buttons));

                                continue;
                            }
                        }
                    }
                } while (true);

                return (bannerUrl, null, false);
            }
            finally
            {
                if (displayMsg is not null)
                    await displayMsg.DeleteAsync();

                await statusMessage.DeleteAsync();
            }
        }

        // TODO: Verify embed is not over limits
        // https://discord.com/developers/docs/resources/channel#embed-object
        protected async Task<(DiscordEmbedBuilder?, string?, bool)> GetCustomDiscordEmbedAsync(Partner p,
            ComponentInteractionCreateEventArgs interaction,
            string title, DiscordEmbedBuilder? toEdit = null)
        {
            
            InteractivityExtension? interact = this.Context.Client.GetInteractivity();

            var statusEmbed = new DiscordEmbedBuilder()
                .WithTitle("Partner Bot Setup - Custom Embed")
                .WithDescription("Welcome to the custom embed builder. Please select what modifications you want to make:")
                .AddField("Editor Options", "`add-field`, `remove-field`, `edit-field`, `edit-desc`, `edit-title`, `edit-color`, `edit-image`," +
                " `save` (saves any changes and exits this editor)")
                .WithColor(DiscordColor.Gold);

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed);
            foreach(var set in GetEmbedButtonMenu())
                builder.AddComponents(set);

            var statusMessage = await builder.SendAsync(interaction.Channel);

            try
            {
                DiscordEmbedBuilder displayEmbed;
                if (toEdit is not null)
                    displayEmbed = toEdit;
                else
                    displayEmbed = new DiscordEmbedBuilder()
                        .WithTitle(title);

                DiscordMessage? displayMessage = await this.Context.RespondAsync(displayEmbed);
                DiscordInteraction? innerInteraction = null;
                do
                {
                    if(innerInteraction is not null)
                    {
                        var hookBuilder = new DiscordWebhookBuilder()
                            .AddEmbed(displayEmbed);
                        foreach (var set in GetEmbedButtonMenu())
                            hookBuilder.AddComponents(set);
                        await innerInteraction.EditOriginalResponseAsync(hookBuilder);
                    }

                    bool invalidSelection = false;

                    var response = await GetButtonPressAsync(interact, statusMessage);

                    if (!response.Item2) return (null, null, true);

                    var res = response.Item1;
                    var btnUpdate = new DiscordInteractionResponseBuilder()
                        .AddEmbed(displayEmbed);
                    foreach (var set in GetEmbedButtonMenu(true))
                        btnUpdate.AddComponents(set);
                    await res.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, btnUpdate);

                    innerInteraction = res.Interaction;

                    if (res.Id.Equals("exit"))
                    {
                        await RespondError("Aborting...");
                        return (null, null, true);
                    }
                    else if (res.Id.Equals("save"))
                    {
                        break;
                    }

                    switch (res.Id)
                    {
                        case "add-field":
                            if (!await AddCustomEmbedField(p, interact, res, displayMessage, displayEmbed))
                                return (null, null, true);
                            break;
                        case "remove-field":
                            if (!await RemoveCustomEmbedField(p, interact, res, displayMessage, displayEmbed))
                                return (null, null, true);
                            break;
                        case "edit-field":
                            if (!await EditCustomEmbedField(p, interact, res, displayMessage, displayEmbed))
                                return (null, null, true);
                            break;
                        case "edit-desc":
                            (string?, string?, bool) newDesc = await GetNewMessage(p, res, displayEmbed.Description.GetUrls().Count);
                            if (newDesc.Item3) return (null, null, true);
                            if (newDesc.Item1 is null) return (null, newDesc.Item2, newDesc.Item3);

                            await displayMessage.ModifyAsync(displayEmbed
                                .WithDescription(newDesc.Item1)
                                .Build());
                            break;
                        case "edit-title":
                            string? newTitle = await GetFieldTitle(interact, statusMessage, statusEmbed);
                            if (newTitle is null) return (null, null, true);

                            await displayMessage.ModifyAsync(displayEmbed
                                .WithTitle(newTitle)
                                .Build());
                            break;
                        case "edit-color":
                            (DiscordColor?, string?, bool) newColor = await GetCustomEmbedColorAsync(p, res);
                            if (newColor.Item3) return (null, null, true);
                            if (newColor.Item1 is null) return (null, newColor.Item2, newColor.Item3);

                            await displayMessage.ModifyAsync(displayEmbed
                                .WithColor(newColor.Item1.Value)
                                .Build());
                            break;
                        case "edit-image":
                            (Uri?, string?, bool) newBanner = await GetNewPartnerBanner(res);
                            if (newBanner.Item3) return (null, null, true);
                            if (newBanner.Item1 is null) return (null, newBanner.Item2, newBanner.Item3);

                            await displayMessage.ModifyAsync(displayEmbed
                                .WithImageUrl(newBanner.Item1)
                                .Build());
                            break;
                        default:
                            invalidSelection = true;
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithDescription("**Invalid selection**. Please make sure to select an item that is listed below:")
                                .WithColor(DiscordColor.DarkRed)
                                .Build());
                            break;
                    }

                    if (!invalidSelection)
                        await statusMessage.ModifyAsync(statusEmbed
                            .WithDescription("Welcome to the custom embed builder. Please select what modifications you want to make:")
                            .WithColor(DiscordColor.Gold)
                            .Build());
                } while (true);

                statusEmbed.RemoveFieldAt(0);

                await displayMessage.DeleteAsync();

                return (displayEmbed, null, false);

            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }

        private DiscordButtonComponent[][] GetEmbedButtonMenu(bool disabled = false)
        {
            var buttons = new DiscordButtonComponent[][]
            {
                new DiscordButtonComponent[]
                {
                        new(ButtonStyle.Primary, "edit-desc", "Edit Description", disabled),
                        new(ButtonStyle.Secondary, "edit-title", "Edit Title", disabled),
                        new(ButtonStyle.Primary, "edit-color", "Edit Color", disabled),
                        new(ButtonStyle.Secondary, "edit-image", "Edit Image", disabled),
                },
                new DiscordButtonComponent[]
                {
                        new(ButtonStyle.Primary, "add-field", "Add Field", disabled),
                        new(ButtonStyle.Secondary, "edit-field", "Edit Field", disabled),
                        new(ButtonStyle.Danger, "remove-field", "Remove Field", disabled)
                },
                new DiscordButtonComponent[]
                {
                        new(ButtonStyle.Primary, "save", "Save Embed", disabled),
                        new(ButtonStyle.Secondary, "exit", "Exit Without Saving", disabled),
                },
            };

            return buttons;
        }

        private async Task<bool> AddCustomEmbedField(Partner p, InteractivityExtension interact,
            ComponentInteractionCreateEventArgs interaction,
            DiscordMessage displayMessage, DiscordEmbedBuilder displayEmbed)
        {
            var statusEmbed = new DiscordEmbedBuilder()
                .WithDescription("Enter the title for this field: ")
                .WithColor(DiscordColor.Gold);

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed);

            var statusMessage = await builder.SendAsync(interaction.Channel);

            try
            {

                string? title = await GetFieldTitle(interact, statusMessage, statusEmbed);

                if (title is null) return false;

                (string?, string?, bool) pmsgResult = await GetNewMessage(p, interaction, 0);

                if (pmsgResult.Item3) return false;
                if (pmsgResult.Item1 is null)
                {
                    await statusMessage.ModifyAsync(statusEmbed
                        .WithDescription(pmsgResult.Item2)
                        .Build());
                    return false;
                }

                string desc = pmsgResult.Item1;

                int currentField = displayEmbed.Fields.Count;
                await displayMessage.ModifyAsync(displayEmbed
                    .AddField(title, desc)
                    .Build());

                statusMessage = await statusMessage.ModifyAsync(
                    new DiscordMessageBuilder()
                        .WithEmbed(statusEmbed
                            .WithDescription("Should this field be inline?")
                            .WithColor(DiscordColor.Gold))
                        .AddComponents(new DiscordComponent[]
                        {
                            new DiscordButtonComponent(ButtonStyle.Primary, "yes", "Yes"),
                            new DiscordButtonComponent(ButtonStyle.Secondary, "no", "No")
                        }));

                bool inline = false;
                bool valid = false;
                do
                {
                    var response = await GetButtonPressAsync(interact, statusMessage);

                    if (!response.Item2) return false;

                    var res = response.Item1;

                    await res.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    if (res.Id.Equals("yes"))
                    {
                        valid = true;
                        inline = true;
                    }
                    else if (res.Id.Equals("no"))
                    {
                        valid = true;
                    }
                } while (!valid);

                displayEmbed.Fields[currentField].Inline = inline;

                await displayMessage.ModifyAsync(displayEmbed.Build());

                return true;

            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }

        private async Task<string?> GetFieldTitle(InteractivityExtension interact, DiscordMessage statusMessage, DiscordEmbedBuilder statusEmbed)
        {
            bool valid = false;
            string title = "";
            do
            {
                (InteractivityResult<DiscordMessage>, bool) response = await GetFollowupMessageAsync(interact);

                if (!response.Item2) return null;

                InteractivityResult<DiscordMessage> res = response.Item1;

                if (string.IsNullOrWhiteSpace(res.Result.Content))
                {
                    await statusMessage.ModifyAsync(statusEmbed
                        .WithDescription("A field title cannot be blank or white space.")
                        .WithColor(DiscordColor.DarkRed)
                        .Build());
                }
                else if (res.Result.Content.GetUrls().Count > 0)
                {
                    await statusMessage.ModifyAsync(statusEmbed
                        .WithDescription("A field title cannot contain a link.")
                        .WithColor(DiscordColor.DarkRed)
                        .Build());
                }
                else
                {
                    valid = true;
                    title = res.Result.Content;
                }
            } while (!valid);

            return title;
        }

        private async Task<bool> EditCustomEmbedField(Partner p, InteractivityExtension interact,
            ComponentInteractionCreateEventArgs interaction,
            DiscordMessage displayMessage, DiscordEmbedBuilder displayEmbed)
        {
            if (displayEmbed.Fields.Count <= 0)
            {
                await interaction.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                            .WithDescription("There are no fields to edit")
                            .WithColor(DiscordColor.DarkRed)));

                await Task.Delay(TimeSpan.FromSeconds(2));

                return true;
            }
            else
            {
                
            }

            int fields = displayEmbed.Fields.Count;

            string desc = $"Please select a field to edit:\n\n";
            List<string> items = new();
            int c = 1;
            var buttons = new List<DiscordButtonComponent>();
            foreach (DiscordEmbedField? f in displayEmbed.Fields)
            {
                items.Add($"`{c}` - {f.Name}");
                buttons.Add(new(ButtonStyle.Primary, c.ToString(), c.ToString()));
                c++;
            }

            var statusEmbed = new DiscordEmbedBuilder()
                .WithDescription($"{desc}{string.Join("\n", items)}")
                .WithColor(DiscordColor.Gold);

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed)
                .AddComponents(buttons);

            var statusMessage = await builder.SendAsync(interaction.Channel);

            try
            {

                int field = 0;
                bool valid = false;
                ComponentInteractionCreateEventArgs res;
                do
                {
                    var response = await GetButtonPressAsync(interact, statusMessage);

                    if (!response.Item2) return false;

                    res = response.Item1;
                    _ = res.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);

                    if (res.Id.Equals("exit"))
                    {
                        await RespondError("Aborting field editor...");
                        return true;
                    }
                    else if (int.TryParse(res.Id, out int num))
                    {
                        if (num > 0 && num <= fields)
                        {
                            field = num - 1;
                            valid = true;
                        }
                        else
                        {
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithDescription($"The number selected must be between `1` and `{fields}`")
                                .WithColor(DiscordColor.DarkRed)
                                .Build());
                        }
                    }
                    else
                    {
                        await statusMessage.ModifyAsync(statusEmbed
                            .WithDescription($"The value must be a number between `1` and `{fields}`")
                            .WithColor(DiscordColor.DarkRed)
                            .Build());
                    }
                } while (!valid);

                var editButtons = new DiscordComponent[]
                {
                    new DiscordButtonComponent(ButtonStyle.Primary, "title", "Title"),
                    new DiscordButtonComponent(ButtonStyle.Secondary, "message", "Message"),
                    new DiscordButtonComponent(ButtonStyle.Primary, "toggle-inline", "Toggle Inline"),
                    new DiscordButtonComponent(ButtonStyle.Success, "save", "Save Changes"),
                    new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Exit Without Saving")
                };

                await res.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddComponents(editButtons)
                        .AddEmbed(statusEmbed
                        .WithDescription($"Please select which field edit you would like to do:")
                            .WithColor(DiscordColor.Gold)));

                string? newDesc = null;
                string? newTitle = null;
                bool invertInline = false;
                do
                {
                    var response = await GetButtonPressAsync(interact, statusMessage);

                    if (!response.Item2) return false;

                    res = response.Item1;

                    if (res.Id.Equals("exit"))
                    {
                        await RespondError("Aborting field editor...");
                        return true;
                    }
                    else if (res.Id.Equals("save"))
                    {
                        break;
                    }

                    switch (res.Id)
                    {
                        case "title":
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithDescription("Please enter the new title for this field:")
                                .Build());

                            newTitle = await GetFieldTitle(interact, statusMessage, statusEmbed);
                            break;
                        case "message":
                            (string?, string?, bool) descRes = await GetNewMessage(p, res, displayEmbed.Fields[field].Value.GetUrls().Count);

                            if (descRes.Item3) return false;

                            newDesc = descRes.Item1;
                            break;
                        case "inline":
                        case "toggle-inline":
                            invertInline = !invertInline;
                            break;
                    }

                    if (newDesc is not null)
                        displayEmbed.Fields[field].Value = newDesc;
                    if (newTitle is not null)
                        displayEmbed.Fields[field].Name = newTitle;
                    if (invertInline)
                        displayEmbed.Fields[field].Inline = !displayEmbed.Fields[field].Inline;

                    await displayMessage.ModifyAsync(displayEmbed.Build());
                } while (true);

                return true;
            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }

        private async Task<bool> RemoveCustomEmbedField(Partner p, InteractivityExtension interact,
            ComponentInteractionCreateEventArgs interaction,
            DiscordMessage displayMessage, DiscordEmbedBuilder displayEmbed)
        {
            if (displayEmbed.Fields.Count <= 0)
            {
                await interaction.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                    new DiscordInteractionResponseBuilder()
                        .AddEmbed(new DiscordEmbedBuilder()
                            .WithDescription("There are no fields to edit")
                            .WithColor(DiscordColor.DarkRed)));

                await Task.Delay(TimeSpan.FromSeconds(2));

                return true;
            }
            else
            {
                
            }

            int fields = displayEmbed.Fields.Count;

            string desc = $"Please select a field to delete:\n\n";
            List<string> items = new();
            int c = 1;
            var buttons = new List<DiscordButtonComponent>();
            foreach (DiscordEmbedField? f in displayEmbed.Fields)
            {
                items.Add($"`{c}` - {f.Name}");
                buttons.Add(new(ButtonStyle.Primary, c.ToString(), c.ToString()));
                c++;
            }

            var statusEmbed = new DiscordEmbedBuilder()
                .WithDescription($"{desc}{string.Join("\n", items)}")
                .WithColor(DiscordColor.Gold);

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed)
                .AddComponents(buttons);

            var statusMessage = await builder.SendAsync(interaction.Channel);

            try
            {

                int field = 0;
                bool valid = false;
                do
                {
                    var response = await GetButtonPressAsync(interact, statusMessage);

                    if (!response.Item2) return false;

                    var res = response.Item1;

                    if (int.TryParse(res.Id, out int num))
                    {
                        if (num > 0 && num <= fields)
                        {
                            field = num - 1;
                            valid = true;
                        }
                        else
                        {
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithDescription($"The number entered must be between `1` and `{fields}`")
                                .WithColor(DiscordColor.DarkRed)
                                .Build());
                        }
                    }
                    else
                    {
                        await statusMessage.ModifyAsync(statusEmbed
                            .WithDescription($"The value must be a number between `1` and `{fields}`")
                            .WithColor(DiscordColor.DarkRed)
                            .Build());
                    }
                } while (!valid);

                string? str = displayEmbed.Fields[field].Value;

                p.LinksUsed -= str.GetUrls().Count;

                await displayMessage.ModifyAsync(displayEmbed
                    .RemoveFieldAt(field)
                    .Build());

                return true;

            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }

        protected async Task<(DiscordColor?, string?, bool)> GetCustomEmbedColorAsync(Partner p, ComponentInteractionCreateEventArgs interaction)
        {
            
            InteractivityExtension? interact = this.Context.Client.GetInteractivity();

            var statusEmbed = new DiscordEmbedBuilder()
                .WithTitle("Partner Bot Setup - Color")
                .WithDescription("Welcome to the color selector. Please input a new Hex color, such as `#16f03a`, or a new RGB value such as `22,240,58`.\n\n" +
                "You can use google's [Color Picker](https://www.google.com/search?q=color+picker) to pick a good color.")
                .WithColor(DiscordColor.Sienna);

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed);

            var statusMessage = await builder.SendAsync(interaction.Channel);

            try
            {

                bool done = false;
                DiscordColor? color = null;
                do
                {
                    (InteractivityResult<DiscordMessage>, bool) folloup = await GetFollowupMessageAsync(interact);

                    if (!folloup.Item2) return (null, null, true);

                    InteractivityResult<DiscordMessage> res = folloup.Item1;

                    string? msg = res.Result.Content.Trim().ToLower();

                    if (msg.Equals("exit"))
                    {
                        await RespondError("Setup cancled");
                        return (null, null, true);
                    }

                    string? first = msg.Split(" ", StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();

                    if (first is null)
                    {
                        await statusMessage.ModifyAsync(statusEmbed
                            .WithColor(DiscordColor.DarkRed)
                            .WithDescription("An RGB color value must have three numerical parts between 0 and 255." +
                            " `R,G,B`. Please enter a valid hex or RGB value.")
                            .Build());

                        continue;
                    }

                    if (msg.Contains(","))
                    {
                        string[]? parts = msg.Split(",", StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length < 3)
                        {
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithColor(DiscordColor.DarkRed)
                                .WithDescription("An RGB color value must have three numerical parts between 0 and 255." +
                                " `R,G,B`. Please enter a valid hex or RGB value.\n\n" +
                                "You can use google's [Color Picker](https://www.google.com/search?q=color+picker) to pick valid colors.")
                                .Build());
                        }
                        else
                        {
                            bool valid = true;

                            byte one = 0, two = 0, three = 0;

                            if (valid)
                                valid &= byte.TryParse(parts[0], out one);
                            if (valid)
                                valid &= byte.TryParse(parts[1], out two);
                            if (valid)
                                valid &= byte.TryParse(parts[2], out three);

                            if (valid)
                            {
                                try
                                {
                                    color = new(one, two, three);
                                    done = true;
                                }
                                catch
                                {
                                    await statusMessage.ModifyAsync(statusEmbed
                                        .WithColor(DiscordColor.DarkRed)
                                        .WithDescription("An RGB color value must have three numerical parts between 0 and 255." +
                                        " `R,G,B`. Please enter a valid hex or RGB value.\n\n" +
                                        "You can use google's [Color Picker](https://www.google.com/search?q=color+picker) to pick valid colors.")
                                        .Build());
                                }
                            }
                            else
                            {
                                await statusMessage.ModifyAsync(statusEmbed
                                    .WithColor(DiscordColor.DarkRed)
                                    .WithDescription("An RGB color value must have three numerical parts between 0 and 255." +
                                    " `R,G,B`. Please enter a valid hex or RGB value.\n\n" +
                                    "You can use google's [Color Picker](https://www.google.com/search?q=color+picker) to pick valid colors.")
                                    .Build());
                            }
                        }
                    }
                    else
                    {
                        try
                        {
                            color = new(first);
                            done = true;
                        }
                        catch
                        {
                            await statusMessage.ModifyAsync(statusEmbed
                                .WithColor(DiscordColor.DarkRed)
                                .WithDescription("A Hex value must be 6 alphanumeric values. Please enter a valid hex value." +
                                " `R,G,B`. Please enter a valid hex or RGB value.\n\n" +
                                "You can use google's [Color Picker](https://www.google.com/search?q=color+picker) to pick valid colors.")
                                .Build());
                        }
                    }
                } while (!done);

                return (color, null, false);
            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }

        protected const int TAG_LIMIT = 10;
        protected async Task<(HashSet<string>?, string?, bool)> UpdateTagsAsync
            (Partner p, ComponentInteractionCreateEventArgs interaction)
        {
            InteractivityExtension? interact = this.Context.Client.GetInteractivity(); 

            var statusEmbed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Aquamarine)
                .WithTitle("Partner Bot Setup - Tags")
                .WithDescription("Welcome to the tag editor! Current Tags:\n" +
                $"`{string.Join("`, `", p.Tags)}`" +
                "\n\n" +
                "Please select an option:");

            var buttons = new DiscordComponent[]
            {
                new DiscordButtonComponent(ButtonStyle.Primary, "add", "Add Tags"),
                new DiscordButtonComponent(ButtonStyle.Secondary, "remove", "Remove Tags"),
                new DiscordButtonComponent(ButtonStyle.Success, "save", "Save Tags"),
                new DiscordButtonComponent(ButtonStyle.Danger, "exit", "Exit Without Saving")
            };

            var builder = new DiscordMessageBuilder()
                .WithEmbed(statusEmbed)
                .AddComponents(buttons);

            var statusMessage = await builder.SendAsync(interaction.Channel);

            try
            {
                bool save = false;
                bool errored = false;
                do
                {
                    var followup = await GetButtonPressAsync(interact, statusMessage);

                    if (!followup.Item2) return (null, null, true);

                    var res = followup.Item1;

                    switch (res.Id)
                    {
                        case "exit":
                            await res.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                            await RespondError("Setup cancled");
                            return (null, null, true);

                        case "save":
                            await res.Interaction.CreateResponseAsync(InteractionResponseType.DeferredMessageUpdate);
                            save = true;
                            break;

                        case "add":
                            await res.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                new DiscordInteractionResponseBuilder()
                                    .AddEmbed(statusEmbed
                                        .WithDescription("**Adding Tags**:\n\n" +
                                        "Please enter the tags you would wish to add. Tags are one word, and multiple tags can be separated by spaces.\n\n" +
                                        "**You can have no more than 10 tags.**\n\n" +
                                        $"Current Tags: `{string.Join("`, `", p.Tags)}`")));

                            (InteractivityResult<DiscordMessage>, bool) addFollowup = await GetFollowupMessageAsync(interact);

                            if (!addFollowup.Item2) return (null, null, true);

                            InteractivityResult<DiscordMessage> addRes = addFollowup.Item1;

                            string[]? addTags = addRes.Result.Content.Trim().ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries);

                            if (p.Tags.Count + addTags.Length > TAG_LIMIT)
                            {
                                await statusMessage.ModifyAsync(statusEmbed
                                    .WithDescription("The amount of tags added place your tags over the limit of 10 tags. Please try adding less tags.")
                                    .Build());

                                await res.Interaction.EditOriginalResponseAsync(
                                    new DiscordWebhookBuilder()
                                        .AddEmbed(statusEmbed
                                            .WithDescription("The amount of tags added place your tags over the limit of 10 tags. Please try adding less tags.\n\n" +
                                            $"Current Tags: `{string.Join("`, `", p.Tags)}`"))
                                        .AddComponents(buttons));

                                errored = true;
                            }
                            else
                            {
                                p.Tags.UnionWith(addTags);
                            }

                            break;

                        case "remove":
                        case "del":

                            await res.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage,
                                new DiscordInteractionResponseBuilder()
                                    .AddEmbed(statusEmbed
                                        .WithDescription("**Removing Tags**:\n\n" +
                                        "Please enter the tags you would wish to remove. Tags are one word, and multiple tags can be separated by spaces.\n\n" +
                                        $"Current Tags: `{string.Join("`, `", p.Tags)}`")));

                            (InteractivityResult<DiscordMessage>, bool) delFollowup = await GetFollowupMessageAsync(interact);

                            if (!delFollowup.Item2) return (null, null, true);

                            InteractivityResult<DiscordMessage> delRes = delFollowup.Item1;

                            HashSet<string> delTags = delRes.Result.Content.Trim().ToLower().Split(" ", StringSplitOptions.RemoveEmptyEntries).ToHashSet();

                            delTags.IntersectWith(p.Tags);
                            p.Tags.SymmetricExceptWith(delTags);

                            break;
                    }

                    if (!errored)
                    {
                        await res.Interaction.EditOriginalResponseAsync(
                            new DiscordWebhookBuilder()
                                .AddEmbed(statusEmbed
                                    .WithColor(DiscordColor.Aquamarine)
                                    .WithTitle("Partner Bot Setup - Tags")
                                    .WithDescription("Welcome to the tag editor! Current Tags:\n" +
                                    $"`{string.Join("`, `", p.Tags)}`" +
                                    "\n\n" +
                                    "Please select an option:"))
                                .AddComponents(buttons));
                    }

                    errored = false;

                } while (!save);

                return (p.Tags, null, false);
            }
            finally
            {
                await statusMessage.DeleteAsync();
            }
        }
    }
}
