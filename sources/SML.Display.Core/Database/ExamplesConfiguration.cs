namespace SML.Display.Core.Database;

using Data.Storable;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

public class ExamplesConfiguration : IEntityTypeConfiguration<Example>
{
    public void Configure(EntityTypeBuilder<Example> builder)
    {
        builder.Property(x => x.Id).UseIdentityByDefaultColumn();
        
        builder.Property(a => a.LastUpdated).IsRequired().HasDefaultValue(new DateTimeOffset(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)));
        //INFO Alternative using shadow property
        //builder.Property<DateTime>("LastUpdated").HasDefaultValue(new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc));
    }

}
