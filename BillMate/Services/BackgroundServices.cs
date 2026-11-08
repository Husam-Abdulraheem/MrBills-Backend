
using BillMate.Interfaces;

namespace BillMate.Services
{
    public class BackgroundServices : BackgroundService
    {

        private readonly IServiceProvider _services;
        private readonly TimeSpan _period = TimeSpan.FromHours(12);
        private readonly ILogger<BackgroundServices> _logger;

        public BackgroundServices(IServiceProvider services, ILogger<BackgroundServices> logger)
        {
            _services = services;
            _logger = logger;
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using PeriodicTimer timer = new(_period);
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _services.CreateScope();

                // Subscription Service
                var service = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                await service.CheckAndUpdateSubscriptionsAsync();

                // Bills Service
                var billService = scope.ServiceProvider.GetRequiredService<IBillService>();
                await billService.CleanupArchivedBillsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Error in background service: {ex.Message}");
                }
            }
        }


    }
}
