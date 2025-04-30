using Intellishelf.Domain.Users.DataAccess;
using Microsoft.Extensions.Hosting;

namespace Intellishelf.Api.Services;

public class RefreshTokenCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RefreshTokenCleanupService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    public RefreshTokenCleanupService(
        IServiceProvider serviceProvider,
        ILogger<RefreshTokenCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("Running refresh token cleanup at {Time}", DateTime.UtcNow);
            
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var refreshTokenDao = scope.ServiceProvider.GetRequiredService<IRefreshTokenDao>();
                
                var result = await refreshTokenDao.TryDeleteExpiredTokensAsync();
                
                if (result.IsSuccess)
                    _logger.LogInformation("Successfully cleaned up expired refresh tokens");
                else
                    _logger.LogError("Failed to clean up expired refresh tokens: {Error}", result.Error.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during refresh token cleanup");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
    }
}
