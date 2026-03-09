namespace SML.Display.Core.Database;

using Data.Storable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class VisistConfigConfiguration : IEntityTypeConfiguration<VisitConfig>
{
    public void Configure(EntityTypeBuilder<VisitConfig> builder)
    {
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        
        builder.Property(a => a.LastUpdated).IsRequired().HasDefaultValue(new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));

        builder.Property(a => a.DisplayName).IsRequired().HasMaxLength(255);
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.HasKey(x => x.Id);
        builder.HasIndex(x => x.DisplayName).IsUnique();
        
        builder.Property(a => a.CareUnitIds)
            .HasConversion(
                v => string.Join(',', v),
                v => v.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList()
            )
            .HasAnnotation("Relational:CheckConstraint", "LEN([CareUnitIds]) > 0");

        //INFO Alternative using shadow property
        //builder.Property<DateTime>("LastUpdated").HasDefaultValue(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

}
