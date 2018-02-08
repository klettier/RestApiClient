// Licensed to the Symphony Software Foundation (SSF) under one
// or more contributor license agreements.  See the NOTICE file
// distributed with this work for additional information
// regarding copyright ownership.  The SSF licenses this file
// to you under the Apache License, Version 2.0 (the
// "License"); you may not use this file except in compliance
// with the License.  You may obtain a copy of the License at
// 
// http://www.apache.org/licenses/LICENSE-2.0
// 
// Unless required by applicable law or agreed to in writing,
// software distributed under the License is distributed on an
// "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY
// KIND, either express or implied.  See the License for the
// specific language governing permissions and limitations
// under the License.

namespace SymphonyOSS.RestApiClient.Api.AgentApi
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Factories;
    using Generated.OpenApi.AgentApi;
    using Entities;
    using Microsoft.Extensions.Logging;
    using SymphonyOSS.RestApiClient.Logging;
    using System.ComponentModel;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Abstract superclass for datafeed-type Apis, eg <see cref="Generated.OpenApi.AgentApi.Api.DatafeedApi"/>
    /// and <see cref="Generated.OpenApi.AgentApi.Api.FirehoseApi"/>.
    /// </summary>
    public abstract class AbstractDatafeedApi
    {
        /// <summary>
        /// True if currently listening for incoming messages, false if not.
        /// </summary>
        public bool Listening { get; protected set; }

        public event EventHandler<MessageEventArgs> OnMessage
        {
            add
            {
                listEventDelegates.AddHandler(onMessage, value);
            }
            remove
            {
                listEventDelegates.RemoveHandler(onMessage, value);

                _inflightEvents.TryRemove(value, out Task t);
            }
        }

        public event EventHandler<ConnectionAcceptedEventArgs> OnConnectionAccepted
        {
            add
            {
                listEventDelegates.AddHandler(onConnectionAccepted, value);
            }
            remove
            {
                listEventDelegates.RemoveHandler(onConnectionAccepted, value);

                _inflightEvents.TryRemove(value, out Task t);
            }
        }

        public event EventHandler<ConnectionRequestedEventArgs> OnConnectionRequested
        {
            add
            {
                listEventDelegates.AddHandler(onConnectionRequested, value);
            }
            remove
            {
                listEventDelegates.RemoveHandler(onConnectionRequested, value);

                _inflightEvents.TryRemove(value, out Task t);
            }
        }

        public event EventHandler<UserJoinedRoomEventArgs> OnUserJoinedRoom
        {
            add
            {
                listEventDelegates.AddHandler(onUserJoinedRoom, value);
            }
            remove
            {
                listEventDelegates.RemoveHandler(onUserJoinedRoom, value);

                _inflightEvents.TryRemove(value, out Task t);
            }
        }

        private ILogger Log;

        //https://docs.microsoft.com/en-us/dotnet/standard/events/how-to-handle-multiple-events-using-event-properties
        protected EventHandlerList listEventDelegates = new EventHandlerList();
        static readonly object onConnectionAccepted = new object();
        static readonly object onConnectionRequested = new object();
        static readonly object onMessage = new object();
        static readonly object onUserJoinedRoom = new object();

        protected volatile bool ShouldStop;
        private readonly ConcurrentDictionary<Delegate, Task> _inflightEvents = new ConcurrentDictionary<Delegate, Task>();
        readonly IReadOnlyDictionary<V4EventType, Action<V4Event>> whatToDo;

        /// <summary>
        /// Initializes a new instance of the <see cref="DatafeedApi" /> class.
        /// See <see cref="Factories.AgentApiFactory"/> for conveniently constructing
        /// an instance.
        /// </summary>
        /// <param name="authTokens">Authentication tokens.</param>
        /// <param name="apiExecutor">Execution strategy.</param>
        protected AbstractDatafeedApi()
        {
            Log = ApiLogging.LoggerFactory?.CreateLogger<AbstractDatafeedApi>();

            whatToDo = new ReadOnlyDictionary<V4EventType, Action<V4Event>>
            (
                new Dictionary<V4EventType, Action<V4Event>>
                {
                    { V4EventType.MESSAGESENT, message => Fire(message, ToMessage, (EventHandler<MessageEventArgs>)listEventDelegates[onMessage])},
                    { V4EventType.CONNECTIONREQUESTED, message => Fire(message, ToConnectionRequested, (EventHandler<ConnectionRequestedEventArgs>)listEventDelegates[onConnectionRequested])},
                    { V4EventType.CONNECTIONACCEPTED, message => Fire(message, ToConnectionAccepted, (EventHandler<ConnectionAcceptedEventArgs>)listEventDelegates[onConnectionAccepted])},
                    { V4EventType.USERJOINEDROOM, message => Fire(message, ToUserJoinedRoomEventArgs, (EventHandler<UserJoinedRoomEventArgs>)listEventDelegates[onUserJoinedRoom])}
                }
            );
        }

        /// <summary>
        /// Requests that <see cref="Listen"/> should stop blocking and return control
        /// to the calling thread. Calling <see cref="Stop"/> will not immediately return
        /// control, but wait for the current outstanding request to complete.
        /// </summary>
        public void Stop()
        {
            ShouldStop = true;
        }

        protected Task CreateInvocationTask<EventHandlerArgs>(EventHandler<EventHandlerArgs> evtHandler, EventHandlerArgs args)
        {
            // we must get the pendingTask first before returning to avoid
            // a race condition where the returned task gets added to 
            // _inflightEvents and then in the task we call TryGetValue
            if (!_inflightEvents.TryGetValue(evtHandler, out var pendingTask))
            {
                pendingTask = null;
            }

            return Task.Run(() =>
            {
                if (pendingTask != null)
                {
                    pendingTask.Wait();
                }

                try
                {
                    evtHandler.Invoke(this, args);
                }
                catch (Exception e)
                {
                    Log?.LogWarning(0, e, "Error invoking message handlers");
                }
            });
        }

        void InvokeEventHandlers<EventHandlerArgs>(EventHandler<EventHandlerArgs> evtHandler, EventHandlerArgs args)
        {
            if (evtHandler == null)
            {
                return;
            }

            foreach (var subhandler in evtHandler.GetInvocationList())
            {
                _inflightEvents[subhandler] = CreateInvocationTask(subhandler as EventHandler<EventHandlerArgs>, args);
            }
        }

        private void Fire<T>(V4Event message, Func<V4Event, T> map, EventHandler<T> handler)
        {
            var eventArgs = map(message);

            InvokeEventHandlers(handler, eventArgs);
        }

        protected MessageEventArgs ToMessage(V4Event message)
        {
            return new MessageEventArgs(MessageFactory.Create(message.Payload.MessageSent.Message));
        }

        protected ConnectionRequestedEventArgs ToConnectionRequested(V4Event message)
        {
            User fromUser = UserFactory.Create(message.Initiator.User);
            User toUser = UserFactory.Create(message.Payload.ConnectionRequested.ToUser);

            return new ConnectionRequestedEventArgs(fromUser, toUser);
        }

        protected ConnectionAcceptedEventArgs ToConnectionAccepted(V4Event message)
        {
            User toUser = UserFactory.Create(message.Initiator.User);
            User fromUser = UserFactory.Create(message.Payload.ConnectionAccepted.FromUser);

            return new ConnectionAcceptedEventArgs(fromUser, toUser);
        }

        protected UserJoinedRoomEventArgs ToUserJoinedRoomEventArgs(V4Event message)
        {
            User initiator = message.Initiator?.User == null ? null : UserFactory.Create(message.Initiator.User);
            User userThatjoined = UserFactory.Create(message.Payload.UserJoinedRoom.AffectedUser);
            LiteRoom room = new LiteRoom(message.Payload.UserJoinedRoom.Stream.StreamId, message.Payload.UserJoinedRoom.Stream.RoomName, message.Payload.UserJoinedRoom.Stream.External);

            return new UserJoinedRoomEventArgs(initiator, userThatjoined, room);
        }

        protected void ProcessMessageList(IEnumerable<V4Event> messageList)
        {
            if (messageList == null)
            {
                return;
            }

            foreach (var message in messageList)
            {
                if (message == null ||
                    !message.Type.HasValue)
                {
                    continue;
                }

                whatToDo.TryGetValue(message.Type.Value, out Action<V4Event> action);
                action?.Invoke(message);
            }
        }
    }
}
