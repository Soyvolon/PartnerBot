using System;
using System.Collections.Generic;

#nullable disable

namespace DBConverter
{
    public partial class Guildconfig
    {
        public long GuildId { get; set; }
        public string Prefix { get; set; }
        public string Managers { get; set; }
        public string LogEnabled { get; set; }
        public string LogChannel { get; set; }
    }
}
