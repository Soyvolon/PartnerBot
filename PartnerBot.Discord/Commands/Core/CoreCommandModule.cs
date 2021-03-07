﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;
using DSharpPlus.Entities;
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

        protected async Task<(InteractivityResult<DiscordMessage>, bool)> GetFollowupMessageAsync(InteractivityExtension interact)
        {
            var res = await interact.WaitForMessageAsync(x => x.Author.Id == Context.Member.Id
                    && x.ChannelId == Context.Channel.Id);

            if (res.TimedOut)
            {
                await InteractTimeout("Setup canceled, interaction timed out.");
                return (res, false);
            }

            await res.Result.DeleteAsync();

            return (res, true);
        }

        protected async Task<(DiscordChannel?, string?, bool)> GetNewPartnerChannelAsync(DiscordMessage statusMessage, DiscordEmbedBuilder statusEmbed)
        {
            var interact = Context.Client.GetInteractivity();
            var color = DiscordColor.Purple;

            await statusMessage.ModifyAsync(statusEmbed
                .WithTitle("Partner Bot Setup - Channel")
                .WithDescription("Please select a channel that you would like to receive partner messages in. THis channel will" +
                " receive any messages from other servers when your advertisment is sent out.\n\n" +
                "The channel requires all overwrites in that channel have the following two permissions:" +
                " `View Channel` and `Read Message History`")
                .WithColor(color)
                .Build());

            DiscordChannel? c = null;
            bool valid = false;
            do
            {
                var folloup = await GetFollowupMessageAsync(interact);

                if (!folloup.Item2) return (null, null, true);

                var res = folloup.Item1;

                if(res.Result.Content.Trim().ToLower().Equals("exit"))
                {
                    await RespondError("Setup cancled");
                    return (null, null, true);
                }
                else if(res.Result.MentionedChannels.Count <= 0)
                {
                    await statusMessage.ModifyAsync(statusEmbed
                        .WithDescription("No channel was seleceted. Please mention a channel or type `exit` to quit.")
                        .WithColor(DiscordColor.DarkRed)
                        .Build());
                }
                else
                {
                    var mentioned = res.Result.MentionedChannels[0];

                    if(ChannelVerificationService.VerifyChannel(mentioned))
                    {
                        valid = true;
                        c = mentioned;
                    }
                    else
                    {
                        string desc = "**Invalid Channel Setup.**\n" +
                            $"Some overwrites are missing the `View Channel` or `Read Message History` for {mentioned.Mention}";

                        List<string> data = new();
                        List<DiscordOverwrite> invalid = new();

                        foreach(var o in mentioned.PermissionOverwrites)
                        {
                            bool access = o.Allowed.HasPermission(Permissions.AccessChannels);
                            bool read = o.Allowed.HasPermission(Permissions.ReadMessageHistory);

                            if (access && read)
                                continue;

                            invalid.Add(o);

                            string msg = "";

                            if(o.Type == OverwriteType.Member)
                            {
                                var m = await o.GetMemberAsync();

                                msg += $"**Invalid Member: {m.Mention}**\n";
                            }
                            else
                            {
                                var r = await o.GetRoleAsync();

                                msg += $"**Invalid Role: {r.Mention}**\n";
                            }

                            msg += $"{(access ? ":white_check_mark:" : ":x:")} `View Channel`\n" +
                                $"{(read ? ":white_check_mark:" : ":x:")} `Read Message History`";

                            data.Add(msg);
                        }

                        desc += $"**Would you like Partner Bot to fix this channel for you? If so, type `yes`." +
                            $" Otherwise, type `no` to return to channel selection.**\n\n\n" +
                            $"{string.Join("\n\n", data)}";

                        await statusMessage.ModifyAsync(statusEmbed
                            .WithDescription(desc)
                            .WithColor(DiscordColor.Red)
                            .Build());

                        folloup = await GetFollowupMessageAsync(interact);

                        if (!folloup.Item2) return (null, null, true);

                        res = folloup.Item1;

                        if (res.Result.Content.Trim().ToLower().Equals("exit"))
                        {
                            await RespondError("Setup cancled");
                            return (null, null, true);
                        }
                        else if (res.Result.Content.Trim().ToLower().Equals("yes"))
                        {
                            var fix = await ConfigurePartnerChannelPermissions(invalid);

                            if(fix.Item1)
                            {
                                valid = true;
                                c = mentioned;
                            }
                            else
                            {
                                await statusMessage.ModifyAsync(statusEmbed
                                    .WithDescription($"Partner Bot was unable to automatically setup the channel:\n" +
                                    $"{fix.Item2}\n\n" +
                                    $"Please fix the channel manually or select a new channel. Mention a channel to continue.")
                                    .WithColor(DiscordColor.DarkRed)
                                    .Build());
                            }
                        }
                    }
                }
            } while (!valid);

            return (c, null, false);
        }

        protected async Task<(bool, string?)> ConfigurePartnerChannelPermissions(List<DiscordOverwrite> invalid)
        {
            try
            {
                foreach (var o in invalid)
                {
                    await o.UpdateAsync(ChannelVerificationService.RequiredPermissions, reason: "Partner Bot Auto Channel Setup");
                }
            }
            catch (UnauthorizedException)
            {
                return (false, "Partner bot does not have permission to modify some channel overwrites.");
            }
            catch (Exception ex)
            {
                return (false, $"Some other error occoured: {ex.Message}");
            }

            return (true, null);
        }

        protected async Task<(string?, string?, bool)> GetNewPartnerMessage(Partner p, DiscordMessage statusMessage, DiscordEmbedBuilder statusEmbed)
        {
            var interact = Context.Client.GetInteractivity();

            await statusMessage.ModifyAsync(statusEmbed
                .WithTitle("Partner Bot Setup - Message")
                .WithDescription("Welcome to the Partner Message setter. Please enter your new partner message.")
                .WithColor(DiscordColor.Aquamarine)
                .Build());

            bool first = true;
            DiscordMessage? pMessage = null;
            string? message = null;
            do
            {
                var response = await GetFollowupMessageAsync(interact);

                if (!response.Item2) return (null, null, true);

                var res = response.Item1;

                var msg = res.Result.Content;

                if (!first && msg.ToLower().Trim().Equals("save"))
                {
                    break;
                }

                var links = msg.GetUrls();

                int c = 0;
                foreach (var l in links)
                {
                    if (c >= p.DonorRank)
                    {
                        msg = msg.Remove(msg.IndexOf(l), l.Length);
                    }
                    else
                    {
                        if(l.ContainsDiscordUrl())
                        {
                            msg = msg.Remove(msg.IndexOf(l), l.Length);
                        }
                    }
                }

                await statusMessage.ModifyAsync(statusEmbed
                    .WithColor(DiscordColor.Aquamarine)
                    .WithDescription("This is your partner message, an invite will be added automatically when it is sent." +
                    " Is this how you would like your message to look? If yes, type `save`, otherwise enter a new Partner Message.")
                    .Build());

                pMessage = await Context.RespondAsync(msg);
                message = msg;

                first = false;
            }
            while (true);

            if(pMessage is not null)
                await pMessage.DeleteAsync();

            return (message, null, false);
        }

        protected async Task<(Uri?, string?, bool)> GetNewPartnerBanner(Partner p, DiscordMessage statusMessage, DiscordEmbedBuilder statusEmbed)
        {
            var interact = Context.Client.GetInteractivity();

            throw new NotImplementedException();
        }

        protected async Task<(DiscordEmbedBuilder?, string?, bool)> GetCustomDiscordEmbedAsync(Partner p, DiscordMessage statusMessage, DiscordEmbedBuilder statusEmbed)
        {
            var interact = Context.Client.GetInteractivity();

            throw new NotImplementedException();
        }

        protected async Task<(DiscordColor?, string?, bool)> GetCustomEmbedColorAsync(Partner p, DiscordMessage statusMessage)
        {
            var interact = Context.Client.GetInteractivity();

            throw new NotImplementedException();
        }
    }
}
