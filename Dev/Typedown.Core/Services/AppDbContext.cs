﻿using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System.IO;
using System.Threading.Tasks;
using Typedown.Core.Models;

namespace Typedown.Core.Services
{
    public class AppDbContext : DbContext
    {
        public DbSet<ExportConfig> ExportConfigs { get; set; }

        public DbSet<FileAccessHistory> FileAccessHistories { get; set; }

        public DbSet<FolderAccessHistory> FolderAccessHistories { get; set; }

        public DbSet<ImageUploadConfig> ImageUploadConfigs { get; set; }


        private readonly string dbPath = Path.Combine(Config.GetLocalFolderPath(), "Storage.db");

        private static readonly object lockMigrateTask = new();

        private static Task migrateTask;

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            var builder = new SqliteConnectionStringBuilder() { DataSource = dbPath };
            options.UseSqlite(builder.ConnectionString);
        }

        public async Task EnsureMigrateAsync()
        {
            lock (lockMigrateTask)
                migrateTask ??= Database.MigrateAsync();
            await migrateTask;
        }

        public static Task<AppDbContext> Create()
        {
            return Task.Run(async () =>
            {
                var ctx = new AppDbContext();
                await ctx.EnsureMigrateAsync();
                return ctx;
            });
        }
    }
}
