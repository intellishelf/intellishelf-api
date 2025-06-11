using Intellishelf.Domain.Users.DataAccess;

namespace Intellishelf.Api.Services;

public class RefreshTokenCleanupService(
    IServiceProvider serviceProvider,
    ILogger<RefreshTokenCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Running refresh token cleanup at {Time}", DateTime.UtcNow);
            
            try
            {
                using var scope = serviceProvider.CreateScope();
                var refreshTokenDao = scope.ServiceProvider.GetRequiredService<IRefreshTokenDao>();
                
                var result = await refreshTokenDao.TryDeleteExpiredTokensAsync();
                
                if (result.IsSuccess)
                    logger.LogInformation("Successfully cleaned up expired refresh tokens");
                else
                    logger.LogError("Failed to clean up expired refresh tokens: {Error}", result.Error.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error occurred during refresh token cleanup");
            }
            
            await Task.Delay(_interval, stoppingToken);
        }
    }
}