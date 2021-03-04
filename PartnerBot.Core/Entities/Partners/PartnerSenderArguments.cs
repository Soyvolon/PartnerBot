﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartnerBot.Core.Entities
{
    public class PartnerSenderArguments
    {
        public int DonorRun { get; set; } = 0;
        public bool IgnoreOwnerMatch { get; set; } = false;
        public bool IgnoreCacheMatch { get; set; } = false;
        public bool DevelopmentStressTest { get; set; } = false;
    }
}
