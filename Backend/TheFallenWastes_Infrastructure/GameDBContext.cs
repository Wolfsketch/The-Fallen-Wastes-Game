using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TheFallenWastes_Domain.Entities;
using TheFallenWastes_Domain.ValueObjects;

namespace TheFallenWastes_Infrastructure
{
    public class GameDbContext : DbContext
    {
        public DbSet<Player> Players { get; set; }
        public DbSet<Settlement> Settlements { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<PlayerRelation> PlayerRelations { get; set; }
        public DbSet<Message> Messages { get; set; }

        public DbSet<Research> Researches { get; set; }
        public DbSet<SettlementResearchState> SettlementResearchStates { get; set; }
        public DbSet<ResearchQueueEntry> ResearchQueueEntries { get; set; }

        public DbSet<SalvageItem> SalvageItems { get; set; }
        public DbSet<SettlementSalvageInventory> SettlementSalvageInventories { get; set; }

        public DbSet<UnitTrainingQueueItem> UnitTrainingQueueItems { get; set; }
        public DbSet<BuildingUpgradeQueueItem> BuildingUpgradeQueueItems { get; set; }

        public GameDbContext(DbContextOptions<GameDbContext> options)
            : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Player -> Settlements
            modelBuilder.Entity<Player>()
                .HasMany(p => p.Settlements)
                .WithOne()
                .HasForeignKey(s => s.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Settlement -> Resources (owned)
            var settlementBuilder = modelBuilder.Entity<Settlement>();

            OwnedNavigationBuilder<Settlement, ResourceStock> resourcesBuilder =
                settlementBuilder.OwnsOne(s => s.Resources);

            resourcesBuilder.Property(r => r.Water).HasColumnName("Water");
            resourcesBuilder.Property(r => r.Food).HasColumnName("Food");
            resourcesBuilder.Property(r => r.Scrap).HasColumnName("Scrap");
            resourcesBuilder.Property(r => r.Fuel).HasColumnName("Fuel");
            resourcesBuilder.Property(r => r.Energy).HasColumnName("Energy");
            resourcesBuilder.Property(r => r.RareTech).HasColumnName("RareTech");

            // Configure UnitInventory as JSON column with value comparer
            settlementBuilder.Property(s => s.UnitInventory)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => string.IsNullOrWhiteSpace(v)
                        ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                        : JsonSerializer.Deserialize<Dictionary<string, int>>(v, (JsonSerializerOptions?)null)
                          ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                )
                .HasColumnType("nvarchar(max)")
                .HasColumnName("UnitInventory")
                .Metadata.SetValueComparer(
                    new ValueComparer<Dictionary<string, int>>(
                        (c1, c2) =>
                            (c1 ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
                            .OrderBy(x => x.Key)
                            .SequenceEqual((c2 ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)).OrderBy(x => x.Key)),
                        c =>
                            (c ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase))
                            .OrderBy(x => x.Key)
                            .Aggregate(0, (a, v) => HashCode.Combine(a, v.Key, v.Value)),
                        c =>
                            c == null
                                ? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                                : c.ToDictionary(k => k.Key, v => v.Value, StringComparer.OrdinalIgnoreCase)
                    )
                );

            // Settlement -> Buildings
            modelBuilder.Entity<Settlement>()
                .HasMany(s => s.Buildings)
                .WithOne()
                .HasForeignKey(b => b.SettlementId)
                .OnDelete(DeleteBehavior.Cascade);

            // Settlement -> UnitTrainingQueue
            modelBuilder.Entity<Settlement>()
                .HasMany(s => s.TrainingQueue)
                .WithOne(q => q.Settlement)
                .HasForeignKey(q => q.SettlementId)
                .OnDelete(DeleteBehavior.Cascade);

            // Building type as string
            modelBuilder.Entity<Building>()
                .Property(b => b.Type)
                .HasConversion<string>()
                .HasMaxLength(50);

            // PlayerRelation
            modelBuilder.Entity<PlayerRelation>()
                .Property(r => r.Type)
                .HasConversion<string>()
                .HasMaxLength(20);

            // Unique constraint: one relation per player pair
            modelBuilder.Entity<PlayerRelation>()
                .HasIndex(r => new { r.PlayerId, r.TargetPlayerId })
                .IsUnique();

            // ----------------------------
            // UNIT TRAINING QUEUE
            // ----------------------------

            modelBuilder.Entity<UnitTrainingQueueItem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.UnitName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Quantity)
                    .IsRequired();

                entity.Property(x => x.PopulationCostPerUnit)
                    .IsRequired();

                entity.Property(x => x.CreatedAtUtc)
                    .IsRequired();

                entity.Property(x => x.StartedAtUtc)
                    .IsRequired();

                entity.Property(x => x.EndsAtUtc)
                    .IsRequired();

                entity.Property(x => x.IsCompleted)
                    .IsRequired();

                entity.Property(x => x.CompletedAtUtc);

                entity.HasIndex(x => new { x.SettlementId, x.IsCompleted, x.EndsAtUtc });
            });

            // ----------------------------
            // BUILDING UPGRADE QUEUE
            // ----------------------------

            modelBuilder.Entity<BuildingUpgradeQueueItem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.BuildingType)
                    .IsRequired()
                    .HasConversion<string>()
                    .HasMaxLength(50);

                entity.Property(x => x.TargetLevel).IsRequired();

                entity.Property(x => x.CostWater);
                entity.Property(x => x.CostFood);
                entity.Property(x => x.CostScrap);
                entity.Property(x => x.CostFuel);
                entity.Property(x => x.CostEnergy);
                entity.Property(x => x.CostRareTech);

                entity.Property(x => x.CreatedAtUtc).IsRequired();

                entity.Property(x => x.IsStarted).IsRequired();
                entity.Property(x => x.StartedAtUtc);
                entity.Property(x => x.EndsAtUtc);

                entity.HasIndex(x => new { x.SettlementId, x.IsStarted, x.TargetLevel });
                entity.Property(x => x.ActiveBuildingId);
                entity.HasIndex(x => x.ActiveBuildingId);
            });

            // ----------------------------
            // RESEARCH
            // ----------------------------

            modelBuilder.Entity<SettlementResearchState>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.SettlementId)
                    .IsUnique();

                entity.Property(x => x.ResearchPoints);
                entity.Property(x => x.MaxConcurrentResearches);

                entity.HasMany(x => x.Researches)
                    .WithOne()
                    .HasForeignKey(x => x.SettlementId)
                    .HasPrincipalKey(x => x.SettlementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Research>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(x => x.Description)
                    .HasMaxLength(1000);

                entity.Property(x => x.Branch)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.HasIndex(x => new { x.SettlementId, x.Key })
                    .IsUnique();
            });

            modelBuilder.Entity<ResearchQueueEntry>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.ResearchKey)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.HasIndex(x => new { x.SettlementResearchStateId, x.SlotIndex, x.QueueOrder });

                entity.HasOne<SettlementResearchState>()
                    .WithMany()
                    .HasForeignKey(x => x.SettlementResearchStateId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // ----------------------------
            // SALVAGE
            // ----------------------------

            modelBuilder.Entity<SettlementSalvageInventory>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.HasIndex(x => x.SettlementId)
                    .IsUnique();

                entity.Property(x => x.StoredRareTech);
                entity.Property(x => x.StoredResearchData);

                entity.HasMany(x => x.Items)
                    .WithOne()
                    .HasForeignKey(x => x.SettlementId)
                    .HasPrincipalKey(x => x.SettlementId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<SalvageItem>(entity =>
            {
                entity.HasKey(x => x.Id);

                entity.Property(x => x.Key)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(x => x.Name)
                    .IsRequired()
                    .HasMaxLength(120);

                entity.Property(x => x.Description)
                    .HasMaxLength(1000);

                entity.Property(x => x.SourceType)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(x => x.Rarity)
                    .IsRequired()
                    .HasMaxLength(30);

                entity.Property(x => x.SpecialOutputKey)
                    .HasMaxLength(100);

                entity.HasIndex(x => new { x.SettlementId, x.Key })
                    .IsUnique();
            });
        }
    }
}