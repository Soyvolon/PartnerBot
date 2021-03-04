using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DSharpPlus;

using PartnerBot.Core.Interfaces;

namespace PartnerBot.Core.Entities
{
    public class PartnerData : Partner, IAsyncExecutable
    {
        public Partner Match { get; internal set; }
        public bool ExtraMessage { get; internal set; }

        public async Task ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}
