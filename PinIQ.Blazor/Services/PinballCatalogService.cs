using System.Net.Http.Json;
using Microsoft.AspNetCore.SignalR.Client;
using Pinball.Entities.Api.Responses.PinballCatalog;

namespace PinIQ.Blazor.Services;

public class PinballCatalogService
{
    private const string OauthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvZ2lkIjoiVGVzdElEIiwib2dpc3MiOiJTZWxmIiwiYXVkIjpbImh0dHBzOi8vbG9jYWxob3N0OjUwMDEiLCJodHRwOi8vbG9jYWxob3N0OjUwMDAiLCJodHRwOi8vbG9jYWxob3N0OjgwMDAiLCJodHRwczovL2xvY2FsaG9zdDo4MDAxIl0sIm5iZiI6MTczODY4MzM2NSwiZXhwIjoxNzM4Njg1MTY1LCJpc3MiOiJwaW5pcS1kZXYifQ.YPI6Qj8bTHlGj5JFOql4kxbEJ__Jv2jXeiQVbzkMiaQ";
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
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {OauthToken}");
        var response = await _httpClient.GetFromJsonAsync<List<CatalogSnapshot>>("api/admin/PinballMachineCatalogSnapshots");
        if (response is null) return [];
        _catalogSnapshotCache = response.ToDictionary(x => x.Id);
        return _catalogSnapshotCache;
    }

    public async Task<CatalogSnapshot?> GetCatalogSnapshot(int id)
    {
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {OauthToken}");
        if (_catalogSnapshotCache.TryGetValue(id, out var value)) return value;
        var response = await _httpClient.GetFromJsonAsync<CatalogSnapshot>($"api/admin/PinballMachineCatalogSnapshots/{id}");
        if (response is null) return null;
        _catalogSnapshotCache.Add(id, response);
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
        await _pinballCatalogNotificationHubConnection.StartAsync();
    }

    public event Action<CatalogSnapshot>? OnCatalogSnapshotAdded;
    
    public event Action<int>? OnCatalogSnapshotRemoved;
}