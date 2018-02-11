using SymphonyOSS.RestApiClient.Api.AgentApi;
using SymphonyOSS.RestApiClient.Entities;
using SymphonyOSS.RestApiClient.Factories;
using SymphonyOSS.RestApiClient.Generated.OpenApi.AgentApi;

namespace SymphonyOSS.RestApiClient.Api.AgentApi
{
    public static class Mapper
    {
        public static MessageEventArgs ToMessage(V4Event message)
        {
            return new MessageEventArgs(MessageFactory.Create(message.Payload.MessageSent.Message));
        }

        public static ConnectionRequestedEventArgs ToConnectionRequested(V4Event message)
        {
            User fromUser = UserFactory.Create(message.Initiator.User);
            User toUser = UserFactory.Create(message.Payload.ConnectionRequested.ToUser);

            return new ConnectionRequestedEventArgs(fromUser, toUser);
        }

        public static ConnectionAcceptedEventArgs ToConnectionAccepted(V4Event message)
        {
            User toUser = UserFactory.Create(message.Initiator.User);
            User fromUser = UserFactory.Create(message.Payload.ConnectionAccepted.FromUser);

            return new ConnectionAcceptedEventArgs(fromUser, toUser);
        }

        public static UserJoinedRoomEventArgs ToUserJoinedRoom(V4Event message)
        {
            User initiator = message.Initiator?.User == null ? null : UserFactory.Create(message.Initiator.User);
            User userThatJoined = UserFactory.Create(message.Payload.UserJoinedRoom.AffectedUser);
            LiteRoom room = new LiteRoom(message.Payload.UserJoinedRoom.Stream.StreamId, message.Payload.UserJoinedRoom.Stream.RoomName, message.Payload.UserJoinedRoom.Stream.External);

            return new UserJoinedRoomEventArgs(initiator, userThatJoined, room);
        }
        public static UserLeftRoomEventArgs ToUserLeftRoom(V4Event message)
        {
            User initiator = message.Initiator?.User == null ? null : UserFactory.Create(message.Initiator.User);
            User userThatLeft = UserFactory.Create(message.Payload.UserLeftRoom.AffectedUser);
            LiteRoom room = new LiteRoom(message.Payload.UserLeftRoom.Stream.StreamId, message.Payload.UserLeftRoom.Stream.RoomName, message.Payload.UserLeftRoom.Stream.External);

            return new UserLeftRoomEventArgs(initiator, userThatLeft, room);
        }

        public static RoomUpdatedEventArgs ToRoomUpdated(V4Event message)
        {
            User initiator = message.Initiator?.User == null ? null : UserFactory.Create(message.Initiator.User);
            RoomProperties roomProperties = ToRoomProperties(message.Payload.RoomUpdated.NewRoomProperties);
            LiteRoom room = new LiteRoom(message.Payload.RoomUpdated.Stream.StreamId, message.Payload.RoomUpdated.Stream.RoomName, message.Payload.RoomUpdated.Stream.External);

            return new RoomUpdatedEventArgs(initiator, roomProperties, room);
        }

        public static RoomProperties ToRoomProperties(V4RoomProperties roomProperties)
        {
            return new RoomProperties
            (
                copyProtected: roomProperties.CopyProtected,
                createdDate: roomProperties.CreatedDate,
                creatorUser: UserFactory.Create(roomProperties.CreatorUser),
                description: roomProperties.Description,
                discoverable: roomProperties.Discoverable,
                external: roomProperties.External,
                membersCanInvite: roomProperties.MembersCanInvite,
                name: roomProperties.Name,
                @public: roomProperties.Public,
                readOnly: roomProperties.ReadOnly
            );
        }
    }
}
