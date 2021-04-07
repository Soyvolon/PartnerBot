using System;
using System.Collections.Generic;

#nullable disable

namespace DBConverter
{
    public partial class Guildban
    {
        public long Id { get; set; }
        public long? OwnerId { get; set; }
        public string Name { get; set; }
        public string Status { get; set; }
        public string Reason { get; set; }
        public string Date { get; set; }
    }
}
