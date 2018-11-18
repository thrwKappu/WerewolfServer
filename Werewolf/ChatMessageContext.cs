
using System;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace DNWS.Werewolf 
{
    /// <summary>
    /// 
    /// </summary>
    public partial  class ChatMessageContext : DbContext
    {
        public DbSet<ChatMessage> ChatMessages {get; set; }
        private static bool _created = false;
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseInMemoryDatabase("ChatMessage");
                optionsBuilder.EnableSensitiveDataLogging();
            }
        }
        public ChatMessageContext()
        {
            if (!_created) {
                _created = true;
                // Database.EnsureDeleted();
                Database.EnsureCreated();
            }
        }
    }
}