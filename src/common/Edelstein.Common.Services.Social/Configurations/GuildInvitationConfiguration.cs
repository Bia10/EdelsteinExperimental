using Edelstein.Common.Services.Social.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Edelstein.Common.Services.Social.Configurations;

public class GuildInvitationConfiguration : IEntityTypeConfiguration<GuildInvitationEntity>
{
    public void Configure(EntityTypeBuilder<GuildInvitationEntity> builder)
    {
        builder.ToTable("guild_invitations");

        builder.HasKey(i => i.ID);

        builder
            .HasOne(i => i.Guild)
            .WithMany(g => g.Invitations)
            .HasForeignKey(i => i.GuildID)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
