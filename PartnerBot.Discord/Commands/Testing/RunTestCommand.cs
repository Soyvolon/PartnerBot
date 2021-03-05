using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

using PartnerBot.Core.Entities;
using PartnerBot.Core.Services;

namespace PartnerBot.Discord.Commands.Testing
{
    public partial class TestCommandGroup : CommandModule
    {
        [Command("run")]
        [Description("Run the saved test case")]
        public async Task RunTestCommandAsync(CommandContext ctx, 
            [Description("Donor run to test as")]
            int donorRun = 0,
            
            [Description("Ignore owner comparison checks?")]
            bool ignoreOwner = false,
            
            [Description("Ignore cache comparison checks?")]
            bool ignoreCache = false)
        {
            var data = await DevelopmentStressTestDataManager.GetDataAsync();

            if(data is null)
            {
                await RespondError("No saved dataset to run a test on.");
                return;
            }

            await RespondSuccess("Starting Test Run...");
            Stopwatch timer = new();
            timer.Start();

            await _sender.ExecuteAsync(new()
            {
                DevelopmentStressTest = true,
                DonorRun = donorRun,
                IgnoreOwnerMatch = ignoreOwner,
                IgnoreCacheMatch = ignoreCache
            });

            await RespondSuccess($"Completed {timer.Elapsed}");
            timer.Stop();
        }
    }
}
