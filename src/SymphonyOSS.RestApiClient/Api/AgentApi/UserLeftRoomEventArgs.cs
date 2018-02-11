namespace SymphonyOSS.RestApiClient.Api.AgentApi
{
    public class UserLeftRoomEventArgs
    {
        public UserLeftRoomEventArgs(Entities.User initiator, Entities.User userThatLeft, Entities.LiteRoom room)
        {
            Initiator = initiator;
            UserThatLeft = userThatLeft;
            Room = room;
        }

        public Entities.User Initiator { get; }
        public Entities.User UserThatLeft { get; }
        public Entities.LiteRoom Room { get; }
    }
}
