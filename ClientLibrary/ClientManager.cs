using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ClientLibrary
{
    public class ClientManager : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private List<Client> _clients = new List<Client>();

        public ClientManager(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _logger = serviceProvider.GetRequiredService<ILogger<ClientManager>>();
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var clients = 2;

            for (var i = 0; i < clients; i++)
            {
                var client = ActivatorUtilities.CreateInstance<Client>(_serviceProvider);
                _ = client.StartAsync(stoppingToken);
                _clients.Add(client);
            }

            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var client in _clients)
            {
                client.StopAsync(cancellationToken);
            }

            return Task.CompletedTask;
        }
    }
}