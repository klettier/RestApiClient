using SymphonyOSS.RestApiClient.Entities;

namespace SymphonyOSS.RestApiClient.Api.AgentApi
{
    public class RoomUpdatedEventArgs
    {
        public RoomUpdatedEventArgs(Entities.User initiator, RoomProperties newRoomProperties, Entities.LiteRoom room)
        {
            Initiator = initiator;
            NewRoomProperties = newRoomProperties;
            Room = room;
        }

        public Entities.User Initiator { get; }
        public RoomProperties NewRoomProperties { get; }
        public Entities.LiteRoom Room { get; }
    }

    public partial class RoomProperties
    {
        public RoomProperties(bool? copyProtected, long? createdDate, User creatorUser, string description, bool? discoverable, bool? external, bool? membersCanInvite, string name, bool? @public, bool? readOnly)
        {
            CopyProtected = copyProtected;
            CreatedDate = createdDate;
            CreatorUser = creatorUser;
            Description = description;
            Discoverable = discoverable;
            External = external;
            MembersCanInvite = membersCanInvite;
            Name = name;
            Public = @public;
            ReadOnly = readOnly;
        }

        public bool? CopyProtected { get; }

        public long? CreatedDate { get; }

        public Entities.User CreatorUser { get; }

        public string Description { get; }

        public bool? Discoverable { get; }

        public bool? External { get; }

        public bool? MembersCanInvite { get; }

        public string Name { get; }

        public bool? Public { get; }

        public bool? ReadOnly { get; }
    }
}
