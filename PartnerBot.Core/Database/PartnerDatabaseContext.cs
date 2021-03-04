using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Microsoft.EntityFrameworkCore;

using PartnerBot.Core.Entities;

namespace PartnerBot.Core.Database
{
    public class PartnerDatabaseContext : DbContext
    {
        public DbSet<Partner> Partners { get; protected set; }

        public PartnerDatabaseContext(DbContextOptions options) : base(options)
        {

        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {


            base.OnModelCreating(modelBuilder);
        }
    }
}
