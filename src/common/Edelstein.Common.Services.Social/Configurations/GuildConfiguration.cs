using Edelstein.Common.Services.Social.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edelstein.Common.Services.Social.Configurations;

public class GuildConfiguration : IEntityTypeConfiguration<GuildEntity>
{
    public void Configure(EntityTypeBuilder<GuildEntity> builder)
    {
        builder.ToTable("guilds");

        builder.HasKey(g => g.ID);

        builder.HasIndex(g => g.Name).IsUnique();
    }
}
