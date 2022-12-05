﻿using GP3.Client.Services;
using GP3.Common.Entities;
using Microsoft.Extensions.Logging;
using MonkeyCache;

namespace GP3.Client.Refit
{
    public class CachedIntegrationApi : IIntegrationApi
    {
        private const string barrelPrefix = "IntegrationAPI";
        private readonly TimeSpan barrelDuration = TimeSpan.FromMinutes(5);
        private readonly IIntegrationApi _api;
        private readonly IBarrel _barrel;
        private readonly IConnectivityService _connectivity;
        private readonly ILogger<CachedIntegrationApi> _logger;
        public CachedIntegrationApi(IIntegrationApi api, IBarrel barrel, IConnectivityService connectivity, ILogger<CachedIntegrationApi> logger)
        {
            _api = api;
            _barrel = barrel;
            _logger = logger;
            _connectivity = connectivity;
        }

        public async Task<IEnumerable<IntegrationCallback>> GetIntegrationsAsync()
        {
            if (!_connectivity.IsConnected())
            {
                _logger.LogInformation("Not connected, returning stale from cache");
                if (_barrel.Exists(barrelPrefix))
                    return _barrel.Get<IEnumerable<IntegrationCallback>>(barrelPrefix);

                _logger.LogError("No connection and not in cache!");
                return default;
            }

            if (!_barrel.IsExpired(barrelPrefix))
            {
                _logger.LogInformation("Un-expired entry in cache, sending");
                return _barrel.Get<IEnumerable<IntegrationCallback>>(barrelPrefix);
            }

            var response = await _api.GetIntegrationsAsync();
            _logger.LogInformation("Adding new info to cache");
            _barrel.Add(key: barrelPrefix, data: response, expireIn: barrelDuration);
            return response;
        }

        public async Task<IntegrationCallback> AddIntegrationAsync(IntegrationCallback integration)
            => await _api.AddIntegrationAsync(integration);

        public async Task RemoveIntegrationAsync(IntegrationCallback integration)
            => await _api.RemoveIntegrationAsync(integration);
    }
}
