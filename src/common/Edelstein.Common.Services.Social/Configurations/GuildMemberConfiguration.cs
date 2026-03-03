using Edelstein.Common.Services.Social.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edelstein.Common.Services.Social.Configurations;

public class GuildMemberConfiguration : IEntityTypeConfiguration<GuildMemberEntity>
{
    public void Configure(EntityTypeBuilder<GuildMemberEntity> builder)
    {
        builder.ToTable("guild_members");

        builder.HasKey(m => m.ID);

        builder
            .HasIndex(m => m.CharacterID)
            .IsUnique();

        builder
            .HasOne(m => m.Guild)
            .WithMany(g => g.Members)
            .HasForeignKey(m => m.GuildID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
