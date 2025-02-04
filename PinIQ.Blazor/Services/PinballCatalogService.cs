using System.Net.Http.Json;
using Pinball.Entities.Api.Responses.PinballCatalog;

namespace PinIQ.Blazor.Services;

public class PinballCatalogService(HttpClient httpClient)
{
    private const string OauthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJvZ2lkIjoiVGVzdElEIiwib2dpc3MiOiJTZWxmIiwiYXVkIjpbImh0dHBzOi8vbG9jYWxob3N0OjUwMDEiLCJodHRwOi8vbG9jYWxob3N0OjUwMDAiLCJodHRwOi8vbG9jYWxob3N0OjgwMDAiLCJodHRwczovL2xvY2FsaG9zdDo4MDAxIl0sIm5iZiI6MTczODYyODE1MiwiZXhwIjoxNzM4NjI5OTUyLCJpc3MiOiJwaW5pcS1kZXYifQ.pWy8NiSqIc8LWSoN5L4S2t-Cozia6qBI47FiXE6B_bk";
    private Dictionary<int, CatalogSnapshot> _catalogSnapshotCache = new();

    public async Task<List<CatalogSnapshot>> GetCatalogSnapshots()
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {OauthToken}");
        var response = await httpClient.GetFromJsonAsync<List<CatalogSnapshot>>("api/admin/PinballMachineCatalogSnapshots");
        if (response is null) return [];
        _catalogSnapshotCache = response.ToDictionary(x => x.Id);
        return response;
    }

    public async Task<CatalogSnapshot?> GetCatalogSnapshot(int id)
    {
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {OauthToken}");
        if (_catalogSnapshotCache.TryGetValue(id, out var value)) return value;
        var response = await httpClient.GetFromJsonAsync<CatalogSnapshot>($"api/admin/PinballMachineCatalogSnapshots/{id}");
        if (response is null) return null;
        _catalogSnapshotCache.Add(id, response);
        return _catalogSnapshotCache[id];
    }
}