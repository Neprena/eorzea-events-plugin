using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace EorzeaEventsPlugin.Api;

// ─── DTOs ─────────────────────────────────────────────────────────────────────

public class PluginVersionInfoDto
{
    [JsonPropertyName("minimum")]        public string Minimum        { get; set; } = "0.0.0";
    [JsonPropertyName("testingMinimum")] public string TestingMinimum { get; set; } = "0.0.0";
}

public class RpSessionDto
{
    [JsonPropertyName("id")]            public string  Id            { get; set; } = string.Empty;
    [JsonPropertyName("title")]         public string  Title         { get; set; } = string.Empty;
    [JsonPropertyName("description")]   public string? Description   { get; set; }
    [JsonPropertyName("location")]      public string  Location      { get; set; } = string.Empty;
    [JsonPropertyName("server")]        public string  Server        { get; set; } = string.Empty;
    [JsonPropertyName("characterName")] public string? CharacterName { get; set; }
    [JsonPropertyName("posX")]          public float?  PosX          { get; set; }
    [JsonPropertyName("posZ")]          public float?  PosZ          { get; set; }
    [JsonPropertyName("ward")]          public int?    Ward          { get; set; }
    [JsonPropertyName("plot")]          public int?    Plot          { get; set; }
    [JsonPropertyName("endedAt")]       public string? EndedAt       { get; set; }
    [JsonPropertyName("territoryId")]   public uint?   TerritoryId   { get; set; }
    [JsonPropertyName("mapId")]         public uint?   MapId         { get; set; }
}

public class EstablishmentSummaryDto
{
    [JsonPropertyName("id")]   public string  Id   { get; set; } = string.Empty;
    [JsonPropertyName("name")] public string  Name { get; set; } = string.Empty;
    [JsonPropertyName("slug")] public string? Slug { get; set; }
}

public class EventDto
{
    [JsonPropertyName("id")]            public string                  Id            { get; set; } = string.Empty;
    [JsonPropertyName("title")]         public string                  Title         { get; set; } = string.Empty;
    [JsonPropertyName("description")]   public string?                 Description   { get; set; }
    [JsonPropertyName("startDate")]     public string                  StartDate     { get; set; } = string.Empty;
    [JsonPropertyName("endDate")]       public string?                 EndDate       { get; set; }
    [JsonPropertyName("isRecurring")]   public bool                    IsRecurring   { get; set; }
    [JsonPropertyName("establishment")] public EstablishmentSummaryDto? Establishment { get; set; }
}

public class SyncshellEntryDto
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;
    [JsonPropertyName("id")]   public string Id   { get; set; } = string.Empty;
}

public class EstablishmentDto
{
    [JsonPropertyName("id")]              public string  Id              { get; set; } = string.Empty;
    [JsonPropertyName("name")]            public string  Name            { get; set; } = string.Empty;
    [JsonPropertyName("slug")]            public string? Slug            { get; set; }
    [JsonPropertyName("description")]     public string? Description     { get; set; }
    [JsonPropertyName("server")]          public string? Server          { get; set; }
    [JsonPropertyName("datacenter")]      public string? Datacenter      { get; set; }
    [JsonPropertyName("address")]         public string? Address         { get; set; }
    [JsonPropertyName("housingType")]     public string? HousingType     { get; set; }
    [JsonPropertyName("district")]        public string? District        { get; set; }
    [JsonPropertyName("ward")]            public int?    Ward            { get; set; }
    [JsonPropertyName("plot")]            public int?    Plot            { get; set; }
    [JsonPropertyName("wing")]            public bool    Wing            { get; set; }
    [JsonPropertyName("apartmentNumber")] public int?    ApartmentNumber { get; set; }
    [JsonPropertyName("syncshells")]      public string  Syncshells      { get; set; } = "[]";
    [JsonPropertyName("discordInvite")]   public string? DiscordInvite   { get; set; }
}

// ─── Request bodies ───────────────────────────────────────────────────────────

public class CreateSessionRequest
{
    [JsonPropertyName("title")]         public string  Title         { get; set; } = string.Empty;
    [JsonPropertyName("description")]   public string? Description   { get; set; }
    [JsonPropertyName("location")]      public string  Location      { get; set; } = string.Empty;
    [JsonPropertyName("server")]        public string  Server        { get; set; } = string.Empty;
    [JsonPropertyName("characterName")] public string? CharacterName { get; set; }
    [JsonPropertyName("posX")]          public float?  PosX          { get; set; }
    [JsonPropertyName("posZ")]          public float?  PosZ          { get; set; }
    [JsonPropertyName("ward")]          public int?    Ward          { get; set; }
    [JsonPropertyName("plot")]          public int?    Plot          { get; set; }
    [JsonPropertyName("duration")]      public int     Duration      { get; set; } = 2;
    [JsonPropertyName("territoryId")]   public uint?   TerritoryId   { get; set; }
    [JsonPropertyName("mapId")]         public uint?   MapId         { get; set; }
}

public class UpdateSessionRequest
{
    [JsonPropertyName("title")]         public string? Title         { get; set; }
    [JsonPropertyName("description")]   public string? Description   { get; set; }
    [JsonPropertyName("location")]      public string? Location      { get; set; }
    [JsonPropertyName("server")]        public string? Server        { get; set; }
    [JsonPropertyName("characterName")] public string? CharacterName { get; set; }
    [JsonPropertyName("posX")]          public float?  PosX          { get; set; }
    [JsonPropertyName("posZ")]          public float?  PosZ          { get; set; }
    [JsonPropertyName("ward")]          public int?    Ward          { get; set; }
    [JsonPropertyName("plot")]          public int?    Plot          { get; set; }
    [JsonPropertyName("duration")]      public int?    Duration      { get; set; }
    [JsonPropertyName("territoryId")]   public uint?   TerritoryId   { get; set; }
    [JsonPropertyName("mapId")]         public uint?   MapId         { get; set; }
}

// ─── Client ──────────────────────────────────────────────────────────────────

public class ApiClient : IDisposable
{
    private readonly HttpClient _http;       // authenticated (user operations)
    private readonly HttpClient _publicHttp; // no auth (public read)

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.Never,
    };

    /// <summary>
    /// Devient false dès qu'un appel authentifié reçoit un 401.
    /// Repasse à true si un appel authentifié réussit (token renouvelé).
    /// </summary>
    public bool IsTokenValid { get; private set; } = true;

    public bool HasToken => _http.DefaultRequestHeaders.Authorization != null;

    public ApiClient(string baseUrl, string? token = null)
    {
        var baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        _publicHttp = new HttpClient { BaseAddress = baseUri };
        _http       = new HttpClient { BaseAddress = baseUri };
        if (!string.IsNullOrWhiteSpace(token))
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }

    private void HandleAuthResponse(System.Net.HttpStatusCode status)
    {
        if (status == System.Net.HttpStatusCode.Unauthorized)
            IsTokenValid = false;
        else if ((int)status < 500)
            IsTokenValid = true;
    }

    // ─── Public read ─────────────────────────────────────────────────────────

    public async Task<PluginVersionInfoDto?> GetVersionInfoAsync(CancellationToken ct = default)
    {
        try
        {
            return await _publicHttp.GetFromJsonAsync<PluginVersionInfoDto>("api/plugin/version", JsonOptions, ct);
        }
        catch { return null; }
    }

    public async Task<List<RpSessionDto>> GetActiveSessionsAsync(CancellationToken ct = default)
    {
        var res = await _publicHttp.GetFromJsonAsync<List<RpSessionDto>>("api/rp-sessions", JsonOptions, ct);
        return res ?? [];
    }

    public async Task<List<EventDto>> GetUpcomingEventsAsync(int days = 14, CancellationToken ct = default)
    {
        var from = Uri.EscapeDataString(DateTime.UtcNow.ToString("o"));
        var to   = Uri.EscapeDataString(DateTime.UtcNow.AddDays(days).ToString("o"));
        var res  = await _publicHttp.GetFromJsonAsync<List<EventDto>>(
            $"api/events?from={from}&to={to}", JsonOptions, ct);
        return res ?? [];
    }

    public async Task<List<EstablishmentDto>> GetEstablishmentsAsync(string? search = null, CancellationToken ct = default)
    {
        var url = "api/establishments";
        if (!string.IsNullOrWhiteSpace(search))
            url += $"?search={Uri.EscapeDataString(search)}";
        var res = await _publicHttp.GetFromJsonAsync<List<EstablishmentDto>>(url, JsonOptions, ct);
        return res ?? [];
    }

    // ─── Authenticated ────────────────────────────────────────────────────────

    public async Task<RpSessionDto?> CreateSessionAsync(CreateSessionRequest req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/rp-sessions", req, ct);
        HandleAuthResponse(res.StatusCode);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            // Essayer d'extraire le champ "error" du JSON
            try
            {
                var err = System.Text.Json.JsonDocument.Parse(body).RootElement;
                if (err.TryGetProperty("error", out var msg))
                    throw new Exception(msg.GetString() ?? $"HTTP {(int)res.StatusCode}");
            }
            catch (System.Text.Json.JsonException) { }
            throw new Exception($"HTTP {(int)res.StatusCode}");
        }
        return await res.Content.ReadFromJsonAsync<RpSessionDto>(JsonOptions, ct);
    }

    public async Task<RpSessionDto?> UpdateSessionAsync(string sessionId, UpdateSessionRequest req, CancellationToken ct = default)
    {
        var res = await _http.PatchAsJsonAsync($"api/rp-sessions/{sessionId}", req, JsonOptions, ct);
        HandleAuthResponse(res.StatusCode);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            try
            {
                var err = System.Text.Json.JsonDocument.Parse(body).RootElement;
                if (err.TryGetProperty("error", out var msg))
                    throw new Exception(msg.GetString() ?? $"HTTP {(int)res.StatusCode}");
            }
            catch (System.Text.Json.JsonException) { }
            throw new Exception($"HTTP {(int)res.StatusCode}");
        }
        return await res.Content.ReadFromJsonAsync<RpSessionDto>(JsonOptions, ct);
    }

    public async Task<bool> EndSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var res = await _http.DeleteAsync($"api/rp-sessions/{sessionId}", ct);
        HandleAuthResponse(res.StatusCode);
        return res.IsSuccessStatusCode;
    }

    // Retourne les IDs des sessions actives appartenant à l'utilisateur authentifié
    public async Task<HashSet<string>> GetMySessionIdsAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/rp-sessions/mine", ct);
            HandleAuthResponse(res.StatusCode);
            if (!res.IsSuccessStatusCode) return [];
            var list = await res.Content.ReadFromJsonAsync<List<string>>(JsonOptions, ct);
            return list != null ? [..list] : [];
        }
        catch { return []; }
    }

    public async Task HeartbeatAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsync("api/plugin/heartbeat", null, ct);
            HandleAuthResponse(res.StatusCode);
        }
        catch { /* silencieux */ }
    }

    // Signale la présence du joueur dans un quartier résidentiel (pour le badge "en ligne" sur le site)
    // Utilise le clientId anonyme — pas de token requis
    public async Task PresenceHeartbeatAsync(uint territoryId, string worldName, string clientId, CancellationToken ct = default)
    {
        try
        {
            var body = new { territoryId, worldName, clientId };
            await _publicHttp.PostAsJsonAsync("api/presence/heartbeat", body, ct);
        }
        catch { /* silencieux */ }
    }

    public async Task<RpSessionDto?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var sessions = await GetActiveSessionsAsync(ct);
        return sessions.FirstOrDefault(s => s.Id == sessionId);
    }

    public void Dispose() { _http.Dispose(); _publicHttp.Dispose(); }
}
