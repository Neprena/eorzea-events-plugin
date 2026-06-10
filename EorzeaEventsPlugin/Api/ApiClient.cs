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
    [JsonPropertyName("emergencyBlock")] public bool   EmergencyBlock { get; set; }
    [JsonPropertyName("message")]        public string? Message       { get; set; }
    [JsonPropertyName("updateUrl")]      public string? UpdateUrl     { get; set; }
}

public class RpSessionAuthorDto
{
    [JsonPropertyName("locale")] public string? Locale { get; set; }
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
    [JsonPropertyName("room")]          public int?    Room          { get; set; }
    [JsonPropertyName("rawPlot")]       public int?    RawPlot       { get; set; }
    [JsonPropertyName("wing")]          public bool?   Wing          { get; set; }
    [JsonPropertyName("endedAt")]       public string? EndedAt       { get; set; }
    [JsonPropertyName("expiresAt")]     public string? ExpiresAt     { get; set; }
    [JsonPropertyName("territoryId")]   public uint?   TerritoryId   { get; set; }
    [JsonPropertyName("mapId")]         public uint?   MapId         { get; set; }
    [JsonPropertyName("author")]        public RpSessionAuthorDto? Author { get; set; }
    // Anti-spam serveur : false tant que la session a < 5 min → pas de notif in-game.
    [JsonPropertyName("notifyEligible")] public bool   NotifyEligible { get; set; }
}

public class EstablishmentSummaryDto
{
    [JsonPropertyName("id")]          public string  Id          { get; set; } = string.Empty;
    [JsonPropertyName("name")]        public string  Name        { get; set; } = string.Empty;
    [JsonPropertyName("slug")]        public string? Slug        { get; set; }
    [JsonPropertyName("banner")]      public string? Banner      { get; set; }
    [JsonPropertyName("server")]      public string? Server      { get; set; }
    [JsonPropertyName("district")]    public string? District    { get; set; }
    [JsonPropertyName("ward")]        public int?    Ward        { get; set; }
    [JsonPropertyName("plot")]        public int?    Plot        { get; set; }
    [JsonPropertyName("housingType")] public string? HousingType { get; set; }
}

public class EventDto
{
    [JsonPropertyName("id")]            public string                  Id            { get; set; } = string.Empty;
    [JsonPropertyName("title")]         public string                  Title         { get; set; } = string.Empty;
    [JsonPropertyName("description")]   public string?                 Description   { get; set; }
    [JsonPropertyName("startDate")]     public string                  StartDate     { get; set; } = string.Empty;
    [JsonPropertyName("endDate")]       public string?                 EndDate       { get; set; }
    [JsonPropertyName("isRecurring")]   public bool                    IsRecurring   { get; set; }
    [JsonPropertyName("isOfficial")]    public bool                    IsOfficial    { get; set; }
    [JsonPropertyName("cancelled")]     public bool                    Cancelled     { get; set; }
    [JsonPropertyName("establishment")] public EstablishmentSummaryDto? Establishment { get; set; }
}

public class OnlineCountDto
{
    [JsonPropertyName("count")] public int Count { get; set; }
}

public class SyncshellEntryDto
{
    [JsonPropertyName("type")]     public string  Type     { get; set; } = string.Empty;
    [JsonPropertyName("name")]     public string? Name     { get; set; }
    [JsonPropertyName("id")]       public string  Id       { get; set; } = string.Empty;
    [JsonPropertyName("password")] public string? Password { get; set; }
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
    [JsonPropertyName("banner")]          public string? Banner          { get; set; }
}

// ─── RP Profile & Availability ────────────────────────────────────────────────

public class RpProfileDto
{
    [JsonPropertyName("rpLevel")]       public string   RpLevel       { get; set; } = string.Empty;
    [JsonPropertyName("approachMode")]  public string   ApproachMode  { get; set; } = string.Empty;
    [JsonPropertyName("languages")]     public string[] Languages     { get; set; } = [];
    [JsonPropertyName("contactMode")]   public string?  ContactMode   { get; set; }
    [JsonPropertyName("sessionLength")] public string?  SessionLength { get; set; }
    [JsonPropertyName("themes")]        public string[] Themes        { get; set; } = [];
}

public class RpAvailabilityEntryDto
{
    [JsonPropertyName("id")]            public string       Id            { get; set; } = string.Empty;
    [JsonPropertyName("characterName")] public string       CharacterName { get; set; } = string.Empty;
    [JsonPropertyName("server")]        public string       Server        { get; set; } = string.Empty;
    [JsonPropertyName("zone")]          public string?      Zone          { get; set; }
    [JsonPropertyName("territoryId")]   public int?         TerritoryId   { get; set; }
    [JsonPropertyName("createdAt")]     public string       CreatedAt     { get; set; } = string.Empty;
    [JsonPropertyName("profile")]       public RpProfileDto? Profile      { get; set; }
}

public class SetRpAvailableRequest
{
    [JsonPropertyName("characterName")] public string  CharacterName { get; set; } = string.Empty;
    [JsonPropertyName("server")]        public string  Server        { get; set; } = string.Empty;
    [JsonPropertyName("zone")]          public string? Zone          { get; set; }
    [JsonPropertyName("territoryId")]   public int?    TerritoryId   { get; set; }
}

public class SaveRpProfileRequest
{
    [JsonPropertyName("rpLevel")]       public string   RpLevel       { get; set; } = string.Empty;
    [JsonPropertyName("approachMode")]  public string   ApproachMode  { get; set; } = string.Empty;
    [JsonPropertyName("languages")]     public string[] Languages     { get; set; } = [];
    [JsonPropertyName("contactMode")]   public string?  ContactMode   { get; set; }
    [JsonPropertyName("sessionLength")] public string?  SessionLength { get; set; }
    [JsonPropertyName("themes")]        public string[] Themes        { get; set; } = [];
}

// ─── Workflow de couplage plugin ↔ compte (web-link) ─────────────────────────

public class LinkStartRequest
{
    [JsonPropertyName("characterName")] public string  CharacterName { get; set; } = string.Empty;
    [JsonPropertyName("worldId")]       public int     WorldId       { get; set; }
    [JsonPropertyName("worldName")]     public string  WorldName     { get; set; } = string.Empty;
    [JsonPropertyName("contentId")]     public string? ContentId     { get; set; }
    [JsonPropertyName("hashedSecret")]  public string  HashedSecret  { get; set; } = string.Empty;
}

public class LinkStartResponse
{
    [JsonPropertyName("sessionId")] public string SessionId { get; set; } = string.Empty;
    [JsonPropertyName("linkUrl")]   public string LinkUrl   { get; set; } = string.Empty;
    [JsonPropertyName("pollUrl")]   public string PollUrl   { get; set; } = string.Empty;
    [JsonPropertyName("expiresAt")] public string ExpiresAt { get; set; } = string.Empty;
}

public class LinkPollCharacterDto
{
    [JsonPropertyName("name")]      public string Name      { get; set; } = string.Empty;
    [JsonPropertyName("worldId")]   public int    WorldId   { get; set; }
    [JsonPropertyName("worldName")] public string WorldName { get; set; } = string.Empty;
}

public class LinkPollResponse
{
    [JsonPropertyName("status")]    public string  Status    { get; set; } = string.Empty; // pending | bound
    [JsonPropertyName("token")]     public string? Token     { get; set; }
    [JsonPropertyName("character")] public LinkPollCharacterDto? Character { get; set; }
}

/// <summary>Résultat synthétique d'un poll, observable par l'UI du plugin.</summary>
public enum LinkPollResult
{
    Pending,
    Bound,
    Expired,
    Error,
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
    [JsonPropertyName("room")]          public int?    Room          { get; set; }
    [JsonPropertyName("rawPlot")]       public int?    RawPlot       { get; set; }
    [JsonPropertyName("duration")]      public int     Duration      { get; set; } = 2;
    [JsonPropertyName("territoryId")]   public uint?   TerritoryId   { get; set; }
    [JsonPropertyName("mapId")]         public uint?   MapId         { get; set; }
    [JsonPropertyName("force")]         public bool    Force         { get; set; } = false;
}

public class ActiveEventConflictException : Exception
{
    public string EstablishmentName { get; }
    public string EventTitle        { get; }
    public ActiveEventConflictException(string estabName, string eventTitle)
        : base("active_event_at_location")
    { EstablishmentName = estabName; EventTitle = eventTitle; }
}

public class ActiveRpConflictException : Exception
{
    public string SessionTitle { get; }
    public string AuthorName   { get; }
    public ActiveRpConflictException(string sessionTitle, string authorName)
        : base("active_rp_at_same_location")
    { SessionTitle = sessionTitle; AuthorName = authorName; }
}

// Blocage dur (IA) : la session sert à promouvoir un évènement déjà annoncé.
// Pas de bypass "force" possible — refus définitif.
public class EventPromotionBlockedException : Exception
{
    public string EstablishmentName { get; }
    public string EventTitle        { get; }
    public EventPromotionBlockedException(string estabName, string eventTitle)
        : base("event_promotion_blocked")
    { EstablishmentName = estabName; EventTitle = eventTitle; }
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
    [JsonPropertyName("room")]          public int?    Room          { get; set; }
    [JsonPropertyName("rawPlot")]       public int?    RawPlot       { get; set; }
    [JsonPropertyName("duration")]      public int?    Duration      { get; set; }
    [JsonPropertyName("territoryId")]   public uint?   TerritoryId   { get; set; }
    [JsonPropertyName("mapId")]         public uint?   MapId         { get; set; }
    [JsonPropertyName("silent")]        public bool?   Silent        { get; set; }
}

// ─── Client ──────────────────────────────────────────────────────────────────

public class ApiClient : IDisposable
{
    private readonly HttpClient _http;       // authenticated (user operations)
    private readonly HttpClient _publicHttp; // no auth (public read)

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Devient false dès qu'un appel authentifié reçoit un 401.
    /// Repasse à true si un appel authentifié réussit (token renouvelé).
    /// </summary>
    public bool IsTokenValid { get; private set; } = true;

    /// <summary>
    /// Le serveur a indiqué via le header X-Token-Deprecated que le token utilisé
    /// est un ancien token de compte (ee_*) — l'utilisateur devrait migrer vers
    /// un token de personnage (ec_*) via le workflow de couplage.
    /// </summary>
    public bool IsTokenDeprecated { get; private set; } = false;

    public bool HasToken => _http.DefaultRequestHeaders.Authorization != null;

    public ApiClient(string baseUrl, string? token = null)
    {
        var baseUri = new Uri(baseUrl.TrimEnd('/') + "/");
        _publicHttp = new HttpClient { BaseAddress = baseUri };
        _http       = new HttpClient { BaseAddress = baseUri };
        SetToken(token);
    }

    /// <summary>
    /// Remplace le token Bearer utilisé pour les requêtes authentifiées.
    /// Vide ou null → désactive l'auth. Reset l'état IsTokenValid / IsTokenDeprecated.
    /// </summary>
    public void SetToken(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        IsTokenValid = true;
        IsTokenDeprecated = false;
    }

    private void HandleAuthResponse(HttpResponseMessage res)
    {
        var status = res.StatusCode;
        if (status == System.Net.HttpStatusCode.Unauthorized)
            IsTokenValid = false;
        else if ((int)status < 500)
            IsTokenValid = true;

        // Avertissement legacy : le serveur signale que ce token doit être migré.
        if (res.Headers.TryGetValues("X-Token-Deprecated", out var values))
        {
            foreach (var v in values)
            {
                if (string.Equals(v, "true", StringComparison.OrdinalIgnoreCase))
                {
                    IsTokenDeprecated = true;
                    break;
                }
            }
        }
    }

    // Backward-compat : ancienne signature acceptant juste le code de statut.
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
        // Start a bit earlier so currently-running events that began before "now"
        // are still included in the returned set.
        var from = Uri.EscapeDataString(DateTime.UtcNow.AddDays(-1).ToString("o"));
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

    public async Task<EstablishmentDto?> GetEstablishmentByIdAsync(string id, CancellationToken ct = default)
    {
        try { return await _publicHttp.GetFromJsonAsync<EstablishmentDto>($"api/establishments/{Uri.EscapeDataString(id)}", JsonOptions, ct); }
        catch { return null; }
    }

    // ─── Authenticated ────────────────────────────────────────────────────────

    public async Task<RpSessionDto?> CreateSessionAsync(CreateSessionRequest req, CancellationToken ct = default)
    {
        var res = await _http.PostAsJsonAsync("api/rp-sessions", req, ct);
        HandleAuthResponse(res.StatusCode);
        if (!res.IsSuccessStatusCode)
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            try
            {
                var err = System.Text.Json.JsonDocument.Parse(body).RootElement;
                // Avertissement : événement actif au même emplacement
                if (err.TryGetProperty("type", out var typeEl))
                {
                    var typeStr = typeEl.GetString();
                    if (typeStr == "active_event_at_location")
                    {
                        var estab = err.TryGetProperty("establishmentName", out var en) ? en.GetString() ?? "" : "";
                        var title = err.TryGetProperty("eventTitle",        out var et) ? et.GetString() ?? "" : "";
                        throw new ActiveEventConflictException(estab, title);
                    }
                    if (typeStr == "active_rp_at_same_location")
                    {
                        var stitle = err.TryGetProperty("sessionTitle", out var st) ? st.GetString() ?? "" : "";
                        var author = err.TryGetProperty("authorName",   out var an) ? an.GetString() ?? "" : "";
                        throw new ActiveRpConflictException(stitle, author);
                    }
                    if (typeStr == "event_promotion_blocked")
                    {
                        var estab = err.TryGetProperty("establishmentName", out var en2) ? en2.GetString() ?? "" : "";
                        var title = err.TryGetProperty("eventTitle",        out var et2) ? et2.GetString() ?? "" : "";
                        throw new EventPromotionBlockedException(estab, title);
                    }
                }
                if (err.TryGetProperty("error", out var msg))
                    throw new Exception(msg.GetString() ?? $"HTTP {(int)res.StatusCode}");
            }
            catch (System.Text.Json.JsonException) { }
            catch (ActiveEventConflictException) { throw; }
            catch (ActiveRpConflictException) { throw; }
            catch (EventPromotionBlockedException) { throw; }
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

    public async Task HeartbeatAsync(
        string? version = null,
        uint? territoryId = null,
        string? worldName = null,
        int? ward = null,
        int? plot = null,
        int? room = null,
        string? characterName = null,
        string? contentId = null,
        CancellationToken ct = default)
    {
        try
        {
            var body = new { version, territoryId, worldName, ward, plot, room, characterName, contentId };
            var res  = await _http.PostAsJsonAsync("api/plugin/heartbeat", body, JsonOptions, ct);
            HandleAuthResponse(res); // capture aussi X-Token-Deprecated
        }
        catch { /* silencieux */ }
    }

    // ─── Workflow de couplage (web-link à la SnowCloak/Mare) ─────────────────

    /// <summary>
    /// Démarre une session de couplage côté serveur. Le plugin doit générer un
    /// secret aléatoire local (32 bytes), passer ici son SHA256 hex, et garder le
    /// secret clair pour le poll. L'utilisateur ouvre ensuite LinkUrl dans son
    /// navigateur, confirme via NextAuth, puis le plugin poll PollUrl avec le
    /// secret clair pour récupérer le token de personnage.
    /// </summary>
    public async Task<LinkStartResponse?> StartLinkAsync(LinkStartRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await _publicHttp.PostAsJsonAsync("api/plugin/link/start", req, JsonOptions, ct);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<LinkStartResponse>(JsonOptions, ct);
        }
        catch { return null; }
    }

    /// <summary>
    /// Interroge le serveur sur l'état d'une session de couplage. Le secret clair
    /// est passé en query pour prouver que le plugin est bien celui qui a démarré
    /// la session. Retourne Bound + le token quand l'utilisateur a confirmé.
    /// Le token n'est lisible qu'une seule fois — appeler à nouveau renvoie Expired.
    /// </summary>
    public async Task<(LinkPollResult result, LinkPollResponse? payload)> PollLinkAsync(
        string sessionId, string plainSecret, CancellationToken ct = default)
    {
        try
        {
            var url = $"api/plugin/link/poll/{Uri.EscapeDataString(sessionId)}?secret={Uri.EscapeDataString(plainSecret)}";
            var res = await _publicHttp.GetAsync(url, ct);
            if (res.StatusCode == System.Net.HttpStatusCode.Gone) return (LinkPollResult.Expired, null);
            if (!res.IsSuccessStatusCode) return (LinkPollResult.Error, null);
            var payload = await res.Content.ReadFromJsonAsync<LinkPollResponse>(JsonOptions, ct);
            if (payload == null) return (LinkPollResult.Error, null);
            return string.Equals(payload.Status, "bound", StringComparison.OrdinalIgnoreCase)
                ? (LinkPollResult.Bound, payload)
                : (LinkPollResult.Pending, payload);
        }
        catch { return (LinkPollResult.Error, null); }
    }

    public async Task<int> GetOnlineCountAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _publicHttp.GetFromJsonAsync<OnlineCountDto>("api/presence/count", JsonOptions, ct);
            return res?.Count ?? 0;
        }
        catch { return 0; }
    }

    // Signale la présence du joueur dans un quartier résidentiel (pour le badge "en ligne" sur le site)
    // Utilise le clientId anonyme — pas de token requis
    public async Task PresenceHeartbeatAsync(
        uint territoryId, string worldName, string clientId,
        int? ward = null, int? plot = null, int? room = null,
        CancellationToken ct = default)
    {
        try
        {
            var body = new { territoryId, worldName, clientId, ward, plot, room };
            await _publicHttp.PostAsJsonAsync("api/presence/heartbeat", body, ct);
        }
        catch { /* silencieux */ }
    }

    public async Task<RpSessionDto?> GetSessionAsync(string sessionId, CancellationToken ct = default)
    {
        var sessions = await GetActiveSessionsAsync(ct);
        return sessions.FirstOrDefault(s => s.Id == sessionId);
    }

    // ─── RP Profile ──────────────────────────────────────────────────────────

    public async Task<RpProfileDto?> GetRpProfileAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/rp-profile", ct);
            HandleAuthResponse(res.StatusCode);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<RpProfileDto>(JsonOptions, ct);
        }
        catch { return null; }
    }

    public async Task<RpProfileDto?> SaveRpProfileAsync(SaveRpProfileRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PutAsJsonAsync("api/rp-profile", req, JsonOptions, ct);
            HandleAuthResponse(res.StatusCode);
            if (!res.IsSuccessStatusCode) return null;
            return await res.Content.ReadFromJsonAsync<RpProfileDto>(JsonOptions, ct);
        }
        catch { return null; }
    }

    // ─── RP Availability ─────────────────────────────────────────────────────

    public async Task<List<RpAvailabilityEntryDto>> GetRpAvailabilitiesAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _publicHttp.GetFromJsonAsync<List<RpAvailabilityEntryDto>>("api/rp-availability", JsonOptions, ct);
            return res ?? [];
        }
        catch { return []; }
    }

    public async Task<bool> SetRpAvailableAsync(SetRpAvailableRequest req, CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/rp-availability", req, JsonOptions, ct);
            HandleAuthResponse(res.StatusCode);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<bool> ClearRpAvailabilityAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.DeleteAsync("api/rp-availability", ct);
            HandleAuthResponse(res.StatusCode);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public void Dispose() { _http.Dispose(); _publicHttp.Dispose(); }
}
