using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace MigrationScript.Models
{
    public class BitcoinContext : DbContext
    {
        public BitcoinContext(DbContextOptions opts)
            : base(opts)
        {

        }

        public DbSet<SegKey> SegKeys { get; set; }
    }
}
