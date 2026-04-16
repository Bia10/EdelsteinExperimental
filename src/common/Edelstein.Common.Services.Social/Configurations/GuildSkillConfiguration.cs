using Edelstein.Common.Services.Social.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edelstein.Common.Services.Social.Configurations;

public class GuildSkillConfiguration : IEntityTypeConfiguration<GuildSkillEntity>
{
    public void Configure(EntityTypeBuilder<GuildSkillEntity> builder)
    {
        builder.ToTable("guild_skills");

        builder.HasKey(s => s.ID);

        builder.HasIndex(s => new { s.GuildID, s.SkillID }).IsUnique();

        builder
            .HasOne(s => s.Guild)
            .WithMany(g => g.Skills)
            .HasForeignKey(s => s.GuildID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
