﻿// Licensed to the Symphony Software Foundation (SSF) under one
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

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace SymphonyOSS.RestApiClient.Tests
{
    using System;
    using Api;
    using Api.PodApi;
    using Authentication;
    using Generated.OpenApi.PodApi;
    using Moq;
    using Xunit;
    using System.Net.Http;

    public class ConnectionApiTest
    {
        private readonly Mock<IApiExecutor> _apiExecutorMock;

        private readonly ConnectionApi _connectionApi;

        public ConnectionApiTest()
        {
            var sessionManagerMock = new Mock<IAuthTokens>();
            sessionManagerMock.Setup(obj => obj.SessionToken).Returns("sessionToken");
            sessionManagerMock.Setup(obj => obj.KeyManagerToken).Returns("keyManagerToken");
            _apiExecutorMock = new Mock<IApiExecutor>();
            _connectionApi = new ConnectionApi(sessionManagerMock.Object, "", new HttpClient(), _apiExecutorMock.Object);
        }

        [Fact]
        public void EnsureGet_uses_api_executor()
        {
            var userId = 12345;
            _apiExecutorMock.Setup(obj => obj.Execute(It.IsAny<Func<string, string, CancellationToken, Task<UserConnection>>>(), "sessionToken", userId.ToString(), default(CancellationToken)))
                .Returns(new UserConnection() {UserId = userId, Status = UserConnectionStatus.ACCEPTED });
            _connectionApi.Get(userId);
            _apiExecutorMock.Verify(obj => obj.Execute(It.IsAny<Func<string, string, CancellationToken, Task<UserConnection>>>(), "sessionToken", userId.ToString(), default(CancellationToken)));
        }

        [Fact]
        public void EnsureList_uses_api_executor_for_null_null()
        {
            _connectionApi.List(null, null);
            _apiExecutorMock.Verify(obj => obj.Execute(It.IsAny<Func<string, Status?, string, CancellationToken, Task<System.Collections.ObjectModel.ObservableCollection<UserConnection>>>>(), "sessionToken", null, null, default(CancellationToken)));
        }

        [Fact]
        public void EnsureList_uses_api_executor_for_status_and_user_ids()
        {
            _connectionApi.List(
                Status.ACCEPTED,
                new List<long>()
                {
                    12345,
                    67890
                });
            _apiExecutorMock.Verify(obj => obj.Execute(It.IsAny<Func<string, Status?, string, CancellationToken, Task<System.Collections.ObjectModel.ObservableCollection<UserConnection>>>>(), "sessionToken", Status.ACCEPTED, "12345,67890", default(CancellationToken)));
        }

        [Fact]
        public void EnsureCreate_uses_api_executor()
        {
            var userId = 12345;
            _apiExecutorMock.Setup(obj => obj.Execute(It.IsAny<Func<string, UserConnectionRequest, CancellationToken, Task<UserConnection>>>(), "sessionToken", It.IsAny<UserConnectionRequest>(), default(CancellationToken)))
                .Returns(new UserConnection() {UserId = userId, Status = UserConnectionStatus.ACCEPTED});
            _connectionApi.Create(userId);
            _apiExecutorMock.Verify(obj => obj.Execute(It.IsAny<Func<string, UserConnectionRequest, CancellationToken, Task<UserConnection>>>(), "sessionToken", It.IsAny<UserConnectionRequest>(), default(CancellationToken)));
        }

        [Fact]
        public void EnsureAccept_uses_api_executor()
        {
            var userId = 12345;
            _apiExecutorMock.Setup(obj => obj.Execute(It.IsAny<Func<string, UserConnectionRequest, CancellationToken, Task<UserConnection>>>(), "sessionToken", It.IsAny<UserConnectionRequest>(), default(CancellationToken)))
                .Returns(new UserConnection() { UserId = userId, Status = UserConnectionStatus.ACCEPTED });
            _connectionApi.Accept(userId);
            _apiExecutorMock.Verify(obj => obj.Execute(It.IsAny<Func<string, UserConnectionRequest, CancellationToken, Task<UserConnection>>>(), "sessionToken", It.IsAny<UserConnectionRequest>(), default(CancellationToken)));
       }

        [Fact]
        public void EnsureReject_uses_api_executor()
        {
            var userId = 12345;
            _apiExecutorMock.Setup(obj => obj.Execute(It.IsAny<Func<string, UserConnectionRequest, CancellationToken, Task<UserConnection>>>(), "sessionToken", It.IsAny<UserConnectionRequest>(), default(CancellationToken)))
                .Returns(new UserConnection() { UserId = userId, Status = UserConnectionStatus.ACCEPTED });
            _connectionApi.Reject(userId);
            _apiExecutorMock.Verify(obj => obj.Execute(It.IsAny<Func<string, UserConnectionRequest, CancellationToken, Task<UserConnection>>>(), "sessionToken", It.IsAny<UserConnectionRequest>(), default(CancellationToken)));
        }
    }
}
