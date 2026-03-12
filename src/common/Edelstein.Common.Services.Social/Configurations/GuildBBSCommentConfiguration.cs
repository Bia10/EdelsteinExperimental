using Edelstein.Common.Services.Social.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edelstein.Common.Services.Social.Configurations;

public class GuildBBSCommentConfiguration : IEntityTypeConfiguration<GuildBBSCommentEntity>
{
    public void Configure(EntityTypeBuilder<GuildBBSCommentEntity> builder)
    {
        builder.ToTable("guild_bbs_comments");

        builder.HasKey(c => c.ID);

        builder
            .HasIndex(c => c.PostID);

        builder
            .HasIndex(c => c.GuildID);

        builder
            .Property(c => c.Content)
            .HasMaxLength(255);
    }
}
