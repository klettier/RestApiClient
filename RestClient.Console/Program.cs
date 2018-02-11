using SymphonyOSS.RestApiClient.Generated.OpenApi.AgentApi;
using SymphonyOSS.RestClient.Generated.OpenApi.AuthenticatorApi;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace RestClient.Console
{
    class Program
    {
        const string agentUrl = "https://foundation-dev.symphony.com/agent";
        const string keyauthUrl = "https://foundation-dev-api.symphony.com/keyauth";
        const string sessionauthUrl = "https://foundation-dev-api.symphony.com/sessionauth";

        //Create a file containing one line like: C:\toto.p12;myPassword
        static readonly string[] pass = File.ReadAllLines(@"C:\Symphony\Pass.txt").First().Split(';').ToArray();

        static readonly X509Certificate2 cert = new X509Certificate2(pass[0], pass[1]);
        static Lazy<HttpClient> certClient = new Lazy<HttpClient>(() => CreateHttpClient(() => CreateCertHandler(cert)));
        static Lazy<HttpClient> secuClient = new Lazy<HttpClient>(() => CreateHttpClient(() => CreateSecuHandler(cert)));
        static Lazy<HttpClient> secuClientRetry = new Lazy<HttpClient>(() => CreateHttpClient(() => CreateSecuRetryHandler(cert)));

        static HttpClient CreateHttpClient(Func<HttpMessageHandler> handlerFactory)
        {
            var handler = handlerFactory();

            return new HttpClient(handler);
        }

        static Lazy<IStreamClient> streamClient = new Lazy<IStreamClient>(() =>
        {
            return new StreamClient(agentUrl, secuClientRetry.Value);
        });

        static IAuthenticateClient sessionAuthApi = new AuthenticateClient(sessionauthUrl, certClient.Value);
        static IAuthenticateClient keyAuthApi = new AuthenticateClient(keyauthUrl, certClient.Value);

        static string sessionToken;
        static string keyManagerToken;
        static object synch = new object();

        static SemaphoreSlim mySemaphoreSlim = new SemaphoreSlim(1, 1);
        static DateTimeOffset lastBuild = default;

        static bool CanGoIn()
        {
            return
                lastBuild == default ||
                (DateTimeOffset.Now - lastBuild) > TimeSpan.FromMinutes(1);
        }

        static async Task BuildTokens()
        {
            if (!CanGoIn())
                return;

            await mySemaphoreSlim.WaitAsync();

            try
            {
                if (!CanGoIn())
                    return;

                sessionToken = (await sessionAuthApi.V1Async()).Token1;
                keyManagerToken = (await keyAuthApi.V1Async()).Token1;

                System.Console.WriteLine($"Generate tokens{Environment.NewLine}{nameof(sessionToken)}: {sessionToken}{Environment.NewLine}{nameof(keyManagerToken)}: {keyManagerToken}");

                lastBuild = DateTimeOffset.Now;
            }
            finally
            {
                mySemaphoreSlim.Release();
            }
        }

        static async Task Main(string[] args)
        {
            await BuildTokens();

            var datafeedApi = new DatafeedClient(agentUrl, certClient.Value);
            var datafeed = await datafeedApi.V4CreateAsync(sessionToken, keyManagerToken);

            while (true)
            {
                try
                {
                    System.Console.WriteLine("Start reading !");
                    var events = await datafeedApi.V4ReadAsync(datafeed.Id, sessionToken, keyManagerToken);

                    await Play(events);
                }
                catch (SymphonyOSS.RestApiClient.Generated.OpenApi.AgentApi.SwaggerException e) when (e.StatusCode == "204")
                {
                    System.Console.WriteLine("204 detected");
                }
            }
        }

        static async Task Play(IEnumerable<V4Event> events)
        {
            var tasks =
                events.Select(e => Task.Factory.StartNew(async () => await Route(e)))
                      .ToArray();

            await Task.WhenAll(tasks);
        }

        static async Task Route(V4Event @event)
        {
            if (@event == null ||
               !@event.Type.HasValue)
                return;

            switch (@event.Type.Value)
            {
                case V4EventType.MESSAGESENT:
                    await Talk(@event.Payload.MessageSent);
                    break;
            }
        }

        static async Task Talk(V4MessageSent message)
        {
            System.Console.WriteLine($"Received {message.Message.Message}");

            if (message.Message.User.UserId != 346621040656630)
            {
                using (var sR = new StringReader(message.Message.Message))
                {
                    var doc = XDocument.Load(sR);
                    var messageContent = doc.Root.Value;
                    var messageSubmit = $@"<messageML>You say ""{messageContent}"" to me the bot</messageML>";
                    await streamClient.Value.V4MessageCreateAsync(message.Message.Stream.StreamId, messageSubmit);
                }
            }
        }

        internal static HttpMessageHandler CreateCertHandler(X509Certificate2 cert)
        {
            HttpClientHandler certHandler = new HttpClientHandler();
            certHandler.ClientCertificateOptions = ClientCertificateOption.Manual;
            certHandler.ClientCertificates.Add(cert);

            return certHandler;
        }

        internal static HttpMessageHandler CreateSecuHandler(X509Certificate2 cert)
        {
            HttpMessageHandler certHandler = CreateCertHandler(cert);

            var headersProvider = new(string Key, Func<string> provider)[]
            {
                ("keyManagerToken", () => keyManagerToken),
                ("sessionToken", () => sessionToken)
            };

            return new SecurityHandler(headersProvider, certHandler);
        }

        internal static HttpMessageHandler CreateSecuRetryHandler(X509Certificate2 cert)
        {
            var securityHandler = CreateSecuHandler(cert);

            Func<HttpResponseMessage, Func<Task<HttpResponseMessage>>, Task<HttpResponseMessage>> fallBack =
                async (response, f) =>
                {
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                    {
                        await BuildTokens();
                        return await f();
                    }

                    return response;
                };

            return new RetryHandler(fallBack, securityHandler);
        }

        public class SecurityHandler : DelegatingHandler
        {
            private readonly IEnumerable<(string Key, Func<string> provider)> headersProvider;

            public SecurityHandler(IEnumerable<(string Key, Func<string> provider)> headersProvider, HttpMessageHandler innerHandler) : base(innerHandler)
            {
                this.headersProvider = headersProvider;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                foreach ((string key, Func<string> provider) in headersProvider)
                {
                    request.Headers.Remove(key);

                    var val = provider();
                    request.Headers.Add(key, val);

                    System.Console.WriteLine($"Set header{Environment.NewLine}{key}: {val}");
                }

                return await base.SendAsync(request, cancellationToken);
            }
        }

        public class RetryHandler : DelegatingHandler
        {
            private readonly Func<HttpResponseMessage, Func<Task<HttpResponseMessage>>, Task<HttpResponseMessage>> fallBack;

            public RetryHandler(Func<HttpResponseMessage, Func<Task<HttpResponseMessage>>, Task<HttpResponseMessage>> fallBack, HttpMessageHandler innerHandler) : base(innerHandler)
            {
                this.fallBack = fallBack;
            }

            protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                Func<Task<HttpResponseMessage>> f = async () => await base.SendAsync(request, cancellationToken);

                var t = await f();

                return await fallBack(t, f);
            }
        }
    }
}
