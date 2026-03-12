using Edelstein.Common.Services.Social.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edelstein.Common.Services.Social.Configurations;

public class GuildBBSPostConfiguration : IEntityTypeConfiguration<GuildBBSPostEntity>
{
    public void Configure(EntityTypeBuilder<GuildBBSPostEntity> builder)
    {
        builder.ToTable("guild_bbs_posts");

        builder.HasKey(p => p.ID);

        builder
            .HasOne(p => p.Guild)
            .WithMany()
            .HasForeignKey(p => p.GuildID)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasMany(p => p.Comments)
            .WithOne(c => c.Post)
            .HasForeignKey(c => c.PostID)
            .OnDelete(DeleteBehavior.Cascade);

        builder
            .HasIndex(p => p.GuildID);

        builder
            .Property(p => p.Title)
            .HasMaxLength(25);

        builder
            .Property(p => p.Content)
            .HasMaxLength(255);
    }
}
