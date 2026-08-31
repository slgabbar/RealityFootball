using System.Text.Json;
using RealityFootball.Api.Models;

namespace RealityFootball.Api.Services;

public sealed class YahooTokenStore(IHostEnvironment environment, ILogger<YahooTokenStore> logger)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };
    private readonly string _tokenFilePath = Path.Combine(environment.ContentRootPath, "data", "yahoo-tokens.json");
    private readonly SemaphoreSlim _lock = new(1, 1);

    public async Task<YahooTokens?> GetAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_tokenFilePath))
            {
                return null;
            }

            await using var stream = File.OpenRead(_tokenFilePath);
            return await JsonSerializer.DeserializeAsync<YahooTokens>(stream, cancellationToken: cancellationToken);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveAsync(YahooTokens tokens, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_tokenFilePath)!);

            await using var stream = File.Create(_tokenFilePath);
            await JsonSerializer.SerializeAsync(stream, tokens, JsonOptions, cancellationToken);
            logger.LogInformation("Yahoo tokens saved to {TokenFilePath}", _tokenFilePath);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_tokenFilePath))
            {
                File.Delete(_tokenFilePath);
            }
        }
        finally
        {
            _lock.Release();
        }
    }
}
