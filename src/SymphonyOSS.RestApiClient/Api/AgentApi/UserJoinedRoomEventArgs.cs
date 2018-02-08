using System;
using System.Collections.Generic;
using System.Text;

namespace SymphonyOSS.RestApiClient.Api.AgentApi
{
   public class UserJoinedRoomEventArgs
    {
        public UserJoinedRoomEventArgs(Entities.User initiator, Entities.User userThatjoined, Entities.LiteRoom room)
        {
            Initiator = initiator;
            UserThatjoined = userThatjoined;
            Room = room;
        }

        public Entities.User Initiator { get; }
        public Entities.User UserThatjoined { get; }
        public Entities.LiteRoom Room { get; }
    }
}
