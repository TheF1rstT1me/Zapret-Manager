using Microsoft.EntityFrameworkCore;
using ZapretManager.Core_.Interfaces;
using ZapretManager.Core_.Models;

namespace ZapretManager.Infrastructure_
{
    public class ZapretContext(DbContextOptions<ZapretContext> options): DbContext(options), IZapretContext
    {
        public DbSet<ZapretSettings> Settings => Set<ZapretSettings>();

        public Dictionary<string, List<VersionInfo>> Versions { get; set; } = new()
        {
            ["Zapret"] = new(),
            ["TgWsProxy"] = new()
        };

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ZapretSettings>(e =>
            {
                e.HasKey(e => e.Id);
                e.OwnsOne(e => e.Paths);
                e.OwnsOne(e => e.Versions);
            });
        }

        public async Task ClearDB()
        {
            await base.Database.EnsureDeletedAsync();
            await base.Database.EnsureCreatedAsync();
        }

        public async Task<ZapretSettings> GetOrInitializeSettingsAsync()
        {
            var settings = await Settings.FirstOrDefaultAsync(s => s.Id == 1);

            if (settings == null)
            {
                settings = new ZapretSettings(1);

                Settings.Add(settings);
                await SaveChangesAsync();
            }

            return settings;
        }

        public async Task MigrateAsync()
        {
            await Database.EnsureCreatedAsync();

            try
            {
                await Database.MigrateAsync();
            }
            catch
            {
                await ClearDB();
            }
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return await base.SaveChangesAsync(cancellationToken);
        }

    }
}
