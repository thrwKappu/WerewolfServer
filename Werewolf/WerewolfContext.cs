
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DNWS.Werewolf 
{
    /// <summary>
    /// 
    /// </summary>
    public partial class WerewolfContext : DbContext
    {
        public DbSet<Role> Roles { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Player> Players { get; set; }
        public DbSet<Action> Actions { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        private static bool _created = false;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=werewolf.db");
        }
        public WerewolfContext()
        {
            if (!_created) {
                _created = true;
                Database.EnsureDeleted();
                Database.EnsureCreated();
            }
        }
    }
}