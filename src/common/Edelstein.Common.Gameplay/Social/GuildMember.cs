using Edelstein.Protocol.Gameplay.Social;
using Edelstein.Protocol.Services.Contracts.Social;

namespace Edelstein.Common.Gameplay.Social
{
    /// <summary>
    /// Immutable read-only member snapshot constructed from a gRPC contract.
    /// </summary>
    /// <summary>Immutable read-only member snapshot constructed from a gRPC contract.</summary>
    public class GuildMember : IGuildMember
    {
        public int ID { get; }
        public string Name { get; }
        public int Job { get; }
        public int Level { get; }
        public int Grade { get; }
        public bool Online { get; }
        public int Commitment { get; }
        public int AllianceGrade { get; }

        public GuildMember(GuildMemberContract contract)
        {
            ID = contract.Id;
            Name = contract.Name;
            Job = contract.Job;
            Level = contract.Level;
            Grade = contract.Grade;
            Online = contract.Online;
            Commitment = contract.Commitment;
            AllianceGrade = contract.AllianceGrade;
        }
    }
}
