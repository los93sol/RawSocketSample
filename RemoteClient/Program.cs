﻿using ClientLibrary;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace RemoteClient
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
                    services.Configure<ClientOptions>(options =>
                    {
                        options.ServerHostname = "rawsocketsample";
                        options.ServerPort = 8087;
                        options.Message = "I'm the remote client";
                        options.Delay = TimeSpan.FromSeconds(1);
                    });
                    services.AddHostedService<ClientManager>();
                })
                .Build();

            await host.StartAsync();
            await host.WaitForShutdownAsync();
        }
    }
}