using Microsoft.EntityFrameworkCore;
using ChemicalDispersionModeling.Core.Models;

namespace ChemicalDispersionModeling.Data.Context;

/// <summary>
/// Entity Framework DbContext for Chemical Dispersion Modeling application
/// </summary>
public class ChemicalDispersionContext : DbContext
{
    public ChemicalDispersionContext(DbContextOptions<ChemicalDispersionContext> options)
        : base(options)
    {
    }

    // DbSets for all entities
    public DbSet<Chemical> Chemicals { get; set; }
    public DbSet<WeatherData> WeatherData { get; set; }
    public DbSet<Release> Releases { get; set; }
    public DbSet<Receptor> Receptors { get; set; }
    public DbSet<DispersionResult> DispersionResults { get; set; }
    public DbSet<TerrainData> TerrainData { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure Chemical entity
        modelBuilder.Entity<Chemical>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.CasNumber).HasMaxLength(50);
            entity.Property(e => e.PhysicalState).HasMaxLength(20);
            entity.Property(e => e.ToxicityUnit).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.CasNumber);
        });

        // Configure WeatherData entity
        modelBuilder.Entity<WeatherData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.StationId).IsRequired().HasMaxLength(50);
            entity.Property(e => e.StabilityClass).HasMaxLength(1);
            entity.Property(e => e.Source).HasMaxLength(50);
            
            entity.HasIndex(e => new { e.StationId, e.Timestamp });
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });

        // Configure Release entity
        modelBuilder.Entity<Release>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReleaseType).HasMaxLength(20);
            entity.Property(e => e.Scenario).HasMaxLength(50);
            entity.Property(e => e.ModelingStabilityClass).HasMaxLength(1);
            entity.Property(e => e.Status).HasMaxLength(20);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.CreatedBy).HasMaxLength(100);
            
            // Configure relationships
            entity.HasOne(d => d.Chemical)
                .WithMany(p => p.Releases)
                .HasForeignKey(d => d.ChemicalId)
                .OnDelete(DeleteBehavior.Restrict);
                
            entity.HasOne(d => d.WeatherData)
                .WithMany(p => p.Releases)
                .HasForeignKey(d => d.WeatherDataId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.ChemicalId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });

        // Configure Receptor entity
        modelBuilder.Entity<Receptor>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ReceptorType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            
            // Configure relationships
            entity.HasOne(d => d.Release)
                .WithMany(p => p.Receptors)
                .HasForeignKey(d => d.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasIndex(e => e.ReleaseId);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });

        // Configure DispersionResult entity
        modelBuilder.Entity<DispersionResult>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ConcentrationUnit).HasMaxLength(10);
            entity.Property(e => e.StabilityClass).HasMaxLength(1);
            entity.Property(e => e.RiskLevel).HasMaxLength(20);
            entity.Property(e => e.ModelUsed).HasMaxLength(50);
            
            // Configure relationships
            entity.HasOne(d => d.Release)
                .WithMany(p => p.DispersionResults)
                .HasForeignKey(d => d.ReleaseId)
                .OnDelete(DeleteBehavior.Cascade);
                
            entity.HasOne(d => d.Receptor)
                .WithMany(p => p.DispersionResults)
                .HasForeignKey(d => d.ReceptorId)
                .OnDelete(DeleteBehavior.SetNull);
                
            entity.HasIndex(e => e.ReleaseId);
            entity.HasIndex(e => e.ReceptorId);
            entity.HasIndex(e => e.CalculationTime);
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
        });

        // Configure TerrainData entity
        modelBuilder.Entity<TerrainData>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.LandUseType).HasMaxLength(50);
            entity.Property(e => e.BuildingType).HasMaxLength(50);
            entity.Property(e => e.DataSource).HasMaxLength(50);
            
            entity.HasIndex(e => new { e.Latitude, e.Longitude });
            entity.HasIndex(e => e.LandUseType);
        });

        // Configure decimal precision for coordinates and measurements
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(double) || property.ClrType == typeof(double?))
                {
                    if (property.Name.Contains("Latitude") || property.Name.Contains("Longitude"))
                    {
                        property.SetPrecision(10);
                        property.SetScale(7);
                    }
                    else
                    {
                        property.SetPrecision(18);
                        property.SetScale(6);
                    }
                }
            }
        }
    }
}