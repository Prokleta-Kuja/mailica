using Microsoft.Extensions.Caching.Memory;

namespace mailica.Services;

public class IpBlackListService
{
    readonly IMemoryCache _cache;

    public IpBlackListService(IMemoryCache cache)
    {
        _cache = cache;
    }
    public async Task<bool> CanReceiveFromIpAddress(string ipAddress)
    {
        // TODO: Check dnsbl
        return await Task.FromResult(false);
    }
    public async Task<bool> CanSendFromIpAddress(string ipAddress)
    {
        // TODO: Check if auth attempts exceeded and country?
        return await Task.FromResult(false);
    }
}