using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Pinball.Entities.Api.Responses.PinballCatalog;

namespace PinIQ.Blazor.Services;

public class PinballCatalogService
{
    private const string OauthToken = "";
    private Dictionary<int, CatalogSnapshot> _catalogSnapshotCache = new();
    private readonly HubConnection _pinballCatalogNotificationHubConnection;
    private readonly HttpClient _httpClient;

    public PinballCatalogService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        
        _pinballCatalogNotificationHubConnection = new HubConnectionBuilder()
            .WithUrl(httpClient.BaseAddress + "catalogNotificationHub")
            .Build();
        
        _pinballCatalogNotificationHubConnection.Closed += async (error) =>
        {
            await Task.Delay(new Random().Next(0, 5) * 1000);
            await _pinballCatalogNotificationHubConnection.StartAsync();
        };
    }

    public async Task<Dictionary<int, CatalogSnapshot>> GetCatalogSnapshots()
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OauthToken);
        var response = await _httpClient.GetFromJsonAsync<List<CatalogSnapshot>>("api/admin/PinballMachineCatalogSnapshots");
        if (response is null) return [];
        _catalogSnapshotCache = response.ToDictionary(x => x.Id);
        return _catalogSnapshotCache;
    }

    public async Task<CatalogSnapshot?> GetCatalogSnapshot(int id)
    {
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", OauthToken);
        if (_catalogSnapshotCache.TryGetValue(id, out var value))
        {
            if (value.Machines.Count != 0)
            {
                return value;
            }
        }
        
        // get individual snapshot including machine data
        var response = await _httpClient.GetFromJsonAsync<CatalogSnapshot>($"api/admin/PinballMachineCatalogSnapshots/{id}");
        if (response is null) return null;
        if (!_catalogSnapshotCache.TryAdd(response.Id, response))
        {
            _catalogSnapshotCache[response.Id] = response;
        }
        return _catalogSnapshotCache[id];
    }

    public async Task StartNotifications()
    {
        _pinballCatalogNotificationHubConnection.On<CatalogSnapshot>("AddCatalogSnapshot", (cs) =>
        {
            _catalogSnapshotCache.Add(cs.Id, cs);
            OnCatalogSnapshotAdded?.Invoke(cs);
        });
        _pinballCatalogNotificationHubConnection.On<int>("RemoveCatalogSnapshot", (id) =>
        {
            _catalogSnapshotCache.Remove(id);
            OnCatalogSnapshotRemoved?.Invoke(id);
        });

        if (_pinballCatalogNotificationHubConnection.State == HubConnectionState.Disconnected)
        {
            await _pinballCatalogNotificationHubConnection.StartAsync();
        }
    }

    public event Action<CatalogSnapshot>? OnCatalogSnapshotAdded;
    
    public event Action<int>? OnCatalogSnapshotRemoved;
}