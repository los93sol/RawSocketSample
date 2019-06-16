using ClientLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace RawSocketSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var host = new HostBuilder()
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                })
                .ConfigureServices(services =>
                {
                    services.AddHostedService<PacketCaptureManager>();

                    services.AddHostedService<SocketServer>();
                    services.Configure<ClientOptions>(options =>
                    {
                        options.ServerHostname = "localhost";
                        options.ServerPort = 8087;
                        options.Message = "I'm the local client";
                    });
                    services.AddHostedService<Client>();
                })
                .Build();

            await host.StartAsync();
            await host.WaitForShutdownAsync();
        }
    }
}
