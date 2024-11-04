using Microsoft.AspNetCore.Mvc.Testing;
using System.Net.WebSockets;
using VsCollaborateApi;

namespace VSCollaborateApi.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public async Task TestMethod1()
        {
            Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
            var app = Program.BuildWebApp(Array.Empty<string>(), builder =>
            {
                builder.WebHost.UseSetting("urls", "https://localhost:6510");
            });
            Task serverTask = app.RunAsync();
            await Task.Delay(1000);

            var httpClient = new HttpClient { BaseAddress = new Uri("https://localhost:6510") };
            string response = await httpClient.GetStringAsync("/");
            Assert.AreEqual("OK", response);

            var clientWebSocket = new ClientWebSocket();
            await clientWebSocket.ConnectAsync(new Uri("wss://localhost:6510/ws"), CancellationToken.None);

            await app.StopAsync();
            GC.KeepAlive(serverTask);
        }
    }
}