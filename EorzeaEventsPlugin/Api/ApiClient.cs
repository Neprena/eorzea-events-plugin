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
    // Position : toujours sérialisée, y compris nulle.
    //
    // Elle est relevée dans le jeu à chaque rafraîchissement, le plugin en est
    // donc la source de vérité. Omise quand elle est nulle, elle laissait le
    // serveur conserver l'ancienne : en quittant une maison pour une zone
    // ouverte, la session restait annoncée à l'adresse du logement, sur le site
    // comme sur Discord.
    [JsonPropertyName("posX")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public float? PosX { get; set; }
    [JsonPropertyName("posZ")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public float? PosZ { get; set; }
    [JsonPropertyName("ward")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public int? Ward { get; set; }
    [JsonPropertyName("plot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public int? Plot { get; set; }
    [JsonPropertyName("room")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public int? Room { get; set; }
    [JsonPropertyName("rawPlot")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)] public int? RawPlot { get; set; }
    [JsonPropertyName("wing")]          public bool?   Wing          { get; set; }
    [JsonPropertyName("endedAt")]       public string? EndedAt       { get; set; }
    [JsonPropertyName("expiresAt")]     public string? ExpiresAt     { get; set; }
    [JsonPropertyName("territoryId")]   public uint?   TerritoryId   { get; set; }
    [JsonPropertyName("mapId")]         public uint?   MapId         { get; set; }
    [JsonPropertyName("author")]        public RpSessionAuthorDto? Author { get; set; }
    // Anti-spam serveur : false tant que la session a < 5 min → pas de notif in-game.
    [JsonPropertyName("notifyEligible")] public bool   NotifyEligible { get; set; }

    /// <summary>
    /// Joueurs présents sur place, comptés par le serveur. C'est ce qui décide
    /// d'aller ou non à un RP ; le serveur payait une agrégation pour rien.
    /// </summary>
    [JsonPropertyName("nearbyCount")] public int NearbyCount { get; set; }
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

    // Sans eux, un lieu en appartement n'a pas d'adresse complète : la parcelle
    // est nulle, et il ne reste que le quartier.
    [JsonPropertyName("apartmentNumber")] public int?  ApartmentNumber { get; set; }
    [JsonPropertyName("wing")]            public bool  Wing            { get; set; }
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

    /// <summary>Affiche propre à l'événement, distincte de la bannière du lieu.</summary>
    [JsonPropertyName("image")] public string? Image { get; set; }

    /// <summary>Bloc iCalendar : « DTSTART:… » puis « RRULE:FREQ=WEEKLY;BYDAY=WE ».</summary>
    [JsonPropertyName("recurrenceRule")] public string? RecurrenceRule { get; set; }

    /// <summary>Compteurs du serveur, dont le nombre d'inscrits.</summary>
    [JsonPropertyName("_count")] public EventCountsDto? Counts { get; set; }

    /// <summary>
    /// Joueurs détectés sur place, même mesure que la page d'accueil du site.
    /// </summary>
    [JsonPropertyName("nearbyCount")] public int NearbyCount { get; set; }
}

public class EventCountsDto
{
    [JsonPropertyName("attendees")] public int Attendees { get; set; }
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

    [JsonPropertyName("rpType")]      public string? RpType      { get; set; } // "full_rp" | "semi_rp"
    [JsonPropertyName("language")]    public string? Language    { get; set; } // "fr" | "en"
    [JsonPropertyName("isNsfw")]      public bool    IsNsfw      { get; set; }
    [JsonPropertyName("isFeatured")]  public bool    IsFeatured  { get; set; }
    [JsonPropertyName("accentColor")] public string? AccentColor { get; set; } // "#RRGGBB"
    [JsonPropertyName("website")]     public string? Website     { get; set; }

    [JsonPropertyName("categories")] public List<EstablishmentCategoryDto>? Categories { get; set; }
    [JsonPropertyName("_count")]     public EstablishmentCountsDto?         Counts     { get; set; }
}

/// <summary>Lien entre un établissement et une catégorie.</summary>
public class EstablishmentCategoryDto
{
    [JsonPropertyName("category")] public CategoryDto? Category { get; set; }
}

public class CategoryDto
{
    [JsonPropertyName("name")]  public string  Name  { get; set; } = string.Empty;
    [JsonPropertyName("emoji")] public string? Emoji { get; set; }
    [JsonPropertyName("color")] public string? Color { get; set; } // "#RRGGBB"
    [JsonPropertyName("group")] public string? Group { get; set; }
}

public class EstablishmentCountsDto
{
    [JsonPropertyName("events")] public int Events { get; set; }
}

// ─── RP Profile & Availability ────────────────────────────────────────────────

/// <summary>
/// Fiche RP d'un personnage.
///
/// Les champs ajoutés avec la fiche par personnage sont tous facultatifs : un
/// serveur antérieur ne les renvoie pas, et le plugin doit rester utilisable
/// dans ce cas.
/// </summary>
public class RpProfileDto
{
    /// <summary>
    /// Personnage propriétaire de la fiche. Renseigné par les réponses publiques
    /// (<c>/api/rp-availability</c> et <c>/api/rp-profile/public/...</c>) : il
    /// permet de recharger la fiche complète et de construire son URL sur le
    /// site, sans jamais chercher un joueur par son nom.
    /// </summary>
    [JsonPropertyName("characterId")]   public string?  CharacterId   { get; set; }

    /// <summary>
    /// La fiche a une page sur le site. Consentement distinct de la visibilité
    /// en jeu : proposer le lien sans vérifier mènerait à une page en 404.
    /// Absent des réponses de la liste des disponibilités, d'où le défaut false.
    /// </summary>
    [JsonPropertyName("hasWebPage")]    public bool     HasWebPage    { get; set; }

    [JsonPropertyName("rpLevel")]       public string   RpLevel       { get; set; } = string.Empty;
    [JsonPropertyName("approachMode")]  public string   ApproachMode  { get; set; } = string.Empty;
    [JsonPropertyName("languages")]     public string[] Languages     { get; set; } = [];
    [JsonPropertyName("contactMode")]   public string?  ContactMode   { get; set; }
    [JsonPropertyName("sessionLength")] public string?  SessionLength { get; set; }
    [JsonPropertyName("themes")]        public string[] Themes        { get; set; } = [];

    [JsonPropertyName("rpName")]       public string?  RpName       { get; set; }
    [JsonPropertyName("nickname")]     public string?  Nickname     { get; set; }
    [JsonPropertyName("pronouns")]     public string?  Pronouns     { get; set; }
    [JsonPropertyName("race")]         public string?  Race         { get; set; }
    [JsonPropertyName("age")]          public string?  Age          { get; set; }
    [JsonPropertyName("origin")]       public string?  Origin       { get; set; }
    [JsonPropertyName("occupation")]   public string?  Occupation   { get; set; }
    [JsonPropertyName("appearance")]   public string?  Appearance   { get; set; }
    [JsonPropertyName("personality")]  public string?  Personality  { get; set; }
    [JsonPropertyName("background")]   public string?  Background   { get; set; }
    [JsonPropertyName("hooks")]        public string[] Hooks        { get; set; } = [];
    [JsonPropertyName("currentQuest")] public string?  CurrentQuest { get; set; }
    [JsonPropertyName("avoidThemes")]  public string[] AvoidThemes  { get; set; } = [];
    [JsonPropertyName("limits")]       public string?  Limits       { get; set; }
    /// <summary>
    /// Consentements et marquage : nullables à dessein.
    ///
    /// Un bool non nullable part toujours dans le corps, avec sa valeur par
    /// défaut, et le serveur écrit tout champ présent. Une requête construite
    /// sans les renseigner republiait donc une fiche masquée, réactivait la page
    /// web et effaçait le marquage sensible. Nuls, ils sont omis, et le serveur
    /// conserve ce qu'il a.
    /// </summary>
    [JsonPropertyName("nsfw")]         public bool     Nsfw         { get; set; }
    [JsonPropertyName("availability")] public string?  Availability { get; set; }
    [JsonPropertyName("externalUrl")]  public string?  ExternalUrl  { get; set; }

    // ─── Visibilité ───────────────────────────────────────────────────────────
    //
    // Trois consentements indépendants, plus l'audience par section. Renseignés
    // par la lecture authentifiée de sa propre fiche (`api/rp-profile`), absents
    // des réponses publiques.

    /// <summary>Visible en jeu : liste des disponibilités, viewer, menu contextuel.</summary>
    [JsonPropertyName("isPublic")] public bool IsPublic { get; set; } = true;

    /// <summary>La fiche a une page sur le site.</summary>
    [JsonPropertyName("webPageEnabled")] public bool WebPageEnabled { get; set; } = true;

    /// <summary>La fiche accepte de figurer dans les moteurs de recherche.</summary>
    [JsonPropertyName("searchIndexable")] public bool SearchIndexable { get; set; }

    /// <summary>
    /// Audience par section, sous la forme brute stockée : un objet JSON
    /// { "&lt;section&gt;": "public" | "owner" }. Une clé absente vaut le défaut de sa
    /// section, défini côté serveur.
    /// </summary>
    [JsonPropertyName("sectionVisibility")] public string? SectionVisibility { get; set; }

    /// <summary>Portrait téléversé depuis le site, cadré en 3:4.</summary>
    [JsonPropertyName("portraitUrl")] public string? PortraitUrl { get; set; }

    [JsonPropertyName("height")] public string? Height { get; set; }
    [JsonPropertyName("build")]  public string? Build  { get; set; }
    [JsonPropertyName("marks")]  public string? Marks  { get; set; }
    [JsonPropertyName("voice")]  public string? Voice  { get; set; }

    [JsonPropertyName("freeCompany")] public string? FreeCompany { get; set; }
    [JsonPropertyName("allegiance")]  public string? Allegiance  { get; set; }
    [JsonPropertyName("deity")]       public string? Deity       { get; set; }

    [JsonPropertyName("quote")]        public string? Quote        { get; set; }
    [JsonPropertyName("themeSongUrl")] public string? ThemeSongUrl { get; set; }

    /// <summary>Relations, en lecture seule ici : elles s'éditent sur le site.</summary>
    [JsonPropertyName("relations")] public RpRelationDto[] Relations { get; set; } = [];
}

/// <summary>Lien vers un autre personnage ou un PNJ.</summary>
public class RpRelationDto
{
    [JsonPropertyName("targetName")] public string  TargetName { get; set; } = string.Empty;
    [JsonPropertyName("kind")]       public string  Kind       { get; set; } = string.Empty;
    [JsonPropertyName("note")]       public string? Note       { get; set; }

    /// <summary>Renseigné par le serveur quand la cible a une fiche publique.</summary>
    [JsonPropertyName("targetCharacterId")] public string? TargetCharacterId { get; set; }
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

/// <summary>
/// Personnage à qui l'on a ouvert sa fiche.
///
/// La liste ne se lit que dans ce sens : le serveur ne dit jamais qui nous a
/// ajouté, ce qui est précisément ce qui rend l'ajout inoffensif pour l'ajouté.
/// <see cref="Mutual"/> fait exception, mais ne révèle l'ajout d'un tiers qu'à
/// quelqu'un qui l'a déjà ajouté lui-même.
/// </summary>
public class RpFriendDto
{
    [JsonPropertyName("characterId")]   public string  CharacterId   { get; set; } = string.Empty;
    [JsonPropertyName("contentIdHash")] public string? ContentIdHash { get; set; }
    [JsonPropertyName("name")]          public string  Name          { get; set; } = string.Empty;
    [JsonPropertyName("worldName")]     public string  WorldName     { get; set; } = string.Empty;

    /// <summary>Nom et monde au moment de l'ajout, pour retrouver un renommé.</summary>
    [JsonPropertyName("addedAsName")]  public string  AddedAsName  { get; set; } = string.Empty;
    [JsonPropertyName("addedAsWorld")] public string  AddedAsWorld { get; set; } = string.Empty;

    [JsonPropertyName("note")]    public string? Note    { get; set; }
    [JsonPropertyName("addedAt")] public string  AddedAt { get; set; } = string.Empty;
    [JsonPropertyName("mutual")]  public bool    Mutual  { get; set; }
}

/// <summary>
/// Désignation d'un personnage à ajouter : son identifiant serveur quand on le
/// connaît déjà, sinon le haché de son ContentId, lu sur un joueur qu'on a sous
/// les yeux. L'identifiant brut d'un tiers ne quitte jamais la machine.
/// </summary>
public class AddRpFriendRequest
{
    [JsonPropertyName("characterId")]   public string? CharacterId   { get; set; }
    [JsonPropertyName("contentIdHash")] public string? ContentIdHash { get; set; }
}

public class SetRpAvailableRequest
{
    [JsonPropertyName("characterName")] public string  CharacterName { get; set; } = string.Empty;
    [JsonPropertyName("server")]        public string  Server        { get; set; } = string.Empty;
    [JsonPropertyName("zone")]          public string? Zone          { get; set; }
    [JsonPropertyName("territoryId")]   public int?    TerritoryId   { get; set; }
}

/// <summary>
/// Corps de mise à jour d'une fiche.
///
/// Les champs que le plugin n'édite pas sont renvoyés tels qu'ils ont été lus :
/// l'enregistrement remplace la fiche entière, les omettre les effacerait.
/// </summary>
/// <summary>
/// Corps du PUT de fiche RP.
///
/// Les listes sont nullables et valent null par défaut : la sérialisation omet
/// les valeurs nulles, et le serveur laisse intact tout champ absent. Un
/// remplissage partiel, comme celui de l'assistant de première configuration, ne
/// peut donc plus vider les accroches ni les thèmes. Une liste explicitement
/// assignée, fût-elle vide, reste envoyée et fait foi.
/// </summary>
public class SaveRpProfileRequest
{
    [JsonPropertyName("rpLevel")]       public string    RpLevel       { get; set; } = string.Empty;
    [JsonPropertyName("approachMode")]  public string    ApproachMode  { get; set; } = string.Empty;
    [JsonPropertyName("languages")]     public string[]  Languages     { get; set; } = [];
    [JsonPropertyName("contactMode")]   public string?   ContactMode   { get; set; }
    [JsonPropertyName("sessionLength")] public string?   SessionLength { get; set; }
    [JsonPropertyName("themes")]        public string[]? Themes        { get; set; }

    [JsonPropertyName("rpName")]       public string?  RpName       { get; set; }
    [JsonPropertyName("nickname")]     public string?  Nickname     { get; set; }
    [JsonPropertyName("pronouns")]     public string?  Pronouns     { get; set; }
    [JsonPropertyName("race")]         public string?  Race         { get; set; }
    [JsonPropertyName("age")]          public string?  Age          { get; set; }
    [JsonPropertyName("origin")]       public string?  Origin       { get; set; }
    [JsonPropertyName("occupation")]   public string?  Occupation   { get; set; }
    [JsonPropertyName("appearance")]   public string?  Appearance   { get; set; }
    [JsonPropertyName("personality")]  public string?  Personality  { get; set; }
    [JsonPropertyName("background")]   public string?  Background   { get; set; }
    [JsonPropertyName("hooks")]        public string[]? Hooks       { get; set; }
    [JsonPropertyName("currentQuest")] public string?  CurrentQuest { get; set; }
    [JsonPropertyName("avoidThemes")]  public string[]? AvoidThemes { get; set; }
    [JsonPropertyName("limits")]       public string?  Limits       { get; set; }
    /// <summary>
    /// Consentements et marquage : nullables à dessein.
    ///
    /// Un bool non nullable part toujours dans le corps, avec sa valeur par
    /// défaut, et le serveur écrit tout champ présent. Une requête construite
    /// sans les renseigner republiait donc une fiche masquée, réactivait la page
    /// web et effaçait le marquage sensible. Nuls, ils sont omis, et le serveur
    /// conserve ce qu'il a.
    /// </summary>
    [JsonPropertyName("nsfw")]         public bool?    Nsfw         { get; set; }
    [JsonPropertyName("availability")] public string?  Availability { get; set; }
    [JsonPropertyName("externalUrl")]  public string?  ExternalUrl  { get; set; }
    [JsonPropertyName("isPublic")]     public bool?    IsPublic     { get; set; }

    [JsonPropertyName("portraitUrl")] public string? PortraitUrl { get; set; }

    [JsonPropertyName("height")] public string? Height { get; set; }
    [JsonPropertyName("build")]  public string? Build  { get; set; }
    [JsonPropertyName("marks")]  public string? Marks  { get; set; }
    [JsonPropertyName("voice")]  public string? Voice  { get; set; }

    [JsonPropertyName("freeCompany")] public string? FreeCompany { get; set; }
    [JsonPropertyName("allegiance")]  public string? Allegiance  { get; set; }
    [JsonPropertyName("deity")]       public string? Deity       { get; set; }

    [JsonPropertyName("quote")]        public string? Quote        { get; set; }
    [JsonPropertyName("themeSongUrl")] public string? ThemeSongUrl { get; set; }

    [JsonPropertyName("webPageEnabled")]  public bool? WebPageEnabled  { get; set; }
    [JsonPropertyName("searchIndexable")] public bool? SearchIndexable { get; set; }

    /// <summary>
    /// Audience par section, en objet et non en chaîne : la route attend un
    /// dictionnaire. Laissé null, il est omis du corps (les options de
    /// sérialisation ignorent les nulls) et le serveur ne réécrit alors pas la
    /// colonne.
    /// </summary>
    [JsonPropertyName("sectionVisibility")]
    public Dictionary<string, string>? SectionVisibility { get; set; }

    /// <summary>
    /// Vocabulaire d'audience que cette version sait afficher.
    ///
    /// Le serveur ne laisse un client écraser que les valeurs qu'il connaît :
    /// sans cette annonce, il tiendrait cet envoi pour celui d'une version
    /// antérieure à l'audience « ami » et refuserait, à juste titre, de la
    /// remplacer. À incrémenter au prochain échelon.
    /// </summary>
    [JsonPropertyName("audienceVocabulary")]
    public int AudienceVocabulary { get; set; } = 2;

    // Les relations sont volontairement absentes : la route ne les remplace que
    // si la clé figure dans le corps, donc ne pas les envoyer les préserve.
    // Les inclure ici ferait effacer côté site tout ce que le jeu ignore.

    /// <summary>
    /// Reprend une fiche lue, pour ne pas effacer ce qui n'est pas édité en jeu.
    /// Tout champ ajouté au modèle doit être recopié ici, sans quoi le premier
    /// enregistrement depuis le jeu le remet à zéro.
    /// </summary>
    public static SaveRpProfileRequest From(RpProfileDto p) => new()
    {
        RpLevel = p.RpLevel, ApproachMode = p.ApproachMode, Languages = p.Languages,
        ContactMode = p.ContactMode, SessionLength = p.SessionLength, Themes = p.Themes,
        RpName = p.RpName, Nickname = p.Nickname, Pronouns = p.Pronouns, Race = p.Race,
        Age = p.Age, Origin = p.Origin, Occupation = p.Occupation,
        Appearance = p.Appearance, Personality = p.Personality, Background = p.Background,
        Hooks = p.Hooks, CurrentQuest = p.CurrentQuest, AvoidThemes = p.AvoidThemes,
        Limits = p.Limits, Nsfw = p.Nsfw, Availability = p.Availability,
        ExternalUrl = p.ExternalUrl, IsPublic = p.IsPublic,
        PortraitUrl = p.PortraitUrl,
        Height = p.Height, Build = p.Build, Marks = p.Marks, Voice = p.Voice,
        FreeCompany = p.FreeCompany, Allegiance = p.Allegiance, Deity = p.Deity,
        Quote = p.Quote, ThemeSongUrl = p.ThemeSongUrl,
        WebPageEnabled = p.WebPageEnabled, SearchIndexable = p.SearchIndexable,
        SectionVisibility = ParseSectionVisibility(p.SectionVisibility),
    };

    /// <summary>
    /// Convertit la chaîne JSON lue en dictionnaire à renvoyer. Une valeur
    /// absente ou illisible donne null, donc la clé est omise du corps et le
    /// serveur conserve ce qu'il a : mieux vaut ne rien dire que d'écraser les
    /// choix de l'utilisateur avec une valeur qu'on n'a pas su relire.
    /// </summary>
    private static Dictionary<string, string>? ParseSectionVisibility(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        try
        {
            var parsed = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            return parsed is { Count: > 0 } ? parsed : null;
        }
        catch { return null; }
    }
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
    // Instance publique : sans elle, le comptage des personnes sur place
    // confond les copies d'une zone divisée.
    [JsonPropertyName("instanceId")]    public uint?   InstanceId    { get; set; }
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
    public string ReasonFr          { get; }
    public string ReasonEn          { get; }
    public EventPromotionBlockedException(string estabName, string eventTitle, string reasonFr, string reasonEn)
        : base("event_promotion_blocked")
    { EstablishmentName = estabName; EventTitle = eventTitle; ReasonFr = reasonFr; ReasonEn = reasonEn; }
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
    // Suit la position : on peut changer d'instance sans bouger d'un pas.
    [JsonPropertyName("instanceId")]    public uint?   InstanceId    { get; set; }
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
                        var estab    = err.TryGetProperty("establishmentName", out var en2) ? en2.GetString() ?? "" : "";
                        var title    = err.TryGetProperty("eventTitle",        out var et2) ? et2.GetString() ?? "" : "";
                        var reasonFr = err.TryGetProperty("reasonFr",          out var rfr) ? rfr.GetString() ?? "" : "";
                        var reasonEn = err.TryGetProperty("reasonEn",          out var ren) ? ren.GetString() ?? "" : "";
                        throw new EventPromotionBlockedException(estab, title, reasonFr, reasonEn);
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
    public async Task<HashSet<string>?> GetMySessionIdsAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/rp-sessions/mine", ct);
            HandleAuthResponse(res.StatusCode);

            // Null sur échec, jamais une liste vide : « aucune session » et « je
            // n'ai pas pu demander » ne se ressemblent pas. Confondre les deux
            // faisait disparaître le bouton de reprise, et donc croire la session
            // terminée alors qu'elle tournait encore côté serveur.
            if (!res.IsSuccessStatusCode) return null;

            var list = await res.Content.ReadFromJsonAsync<List<string>>(JsonOptions, ct);
            return list != null ? [..list] : [];
        }
        catch { return null; }
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
    /// <remarks>
    /// La position et l'instance servent au comptage des personnes sur un RP ouvert
    /// hors logement. Sans elles, le serveur ne sait rapprocher un joueur d'un RP que
    /// par l'adresse housing, et un RP en zone ouverte annonce toujours zéro personne
    /// sur place. Ce sont des coordonnées CARTE (celles affichées en jeu), même repère
    /// que la position d'une session, et non des coordonnées monde.
    /// </remarks>
    public async Task PresenceHeartbeatAsync(
        uint territoryId, string worldName, string clientId,
        int? ward = null, int? plot = null, int? room = null,
        float? posX = null, float? posZ = null, uint? instanceId = null,
        CancellationToken ct = default)
    {
        try
        {
            // Arrondi à la décimale : c'est la précision d'une coordonnée affichée
            // en jeu, et elle suffit largement à mesurer une distance. Inutile de
            // transmettre une position plus fine que ce qu'on en fait.
            var body = new
            {
                territoryId, worldName, clientId, ward, plot, room, instanceId,
                posX = posX.HasValue ? MathF.Round(posX.Value, 1) : (float?)null,
                posZ = posZ.HasValue ? MathF.Round(posZ.Value, 1) : (float?)null,
            };
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

    /// <summary>
    /// Fiche publique d'un autre personnage, plus complète que celle incluse dans
    /// la liste des disponibilités : biographie, relations, traits physiques et
    /// appartenances.
    ///
    /// Passe par le client authentifié : le jeton n'ouvre aucun droit par
    /// lui-même, il permet au serveur de savoir si l'on figure dans la liste
    /// d'amis de cette fiche, et donc de servir les sections réservées aux amis.
    /// Sans jeton, la requête part sans en-tête et la réponse est la version
    /// publique.
    ///
    /// Retourne null si la fiche n'existe pas, n'est pas visible en jeu, ou si le
    /// site est injoignable : l'appelant garde alors les champs déjà en mémoire.
    /// </summary>
    public async Task<RpProfileDto?> GetPublicRpProfileAsync(string characterId,
                                                             bool asFriend = false,
                                                             CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return null;

        try
        {
            // `as=friend` n'est honoré que sur sa propre fiche : c'est l'aperçu,
            // il ne donne aucun accès supplémentaire ailleurs.
            var query = asFriend ? "?as=friend" : string.Empty;
            var res = await _http.GetAsync(
                $"api/rp-profile/public/{Uri.EscapeDataString(characterId)}{query}", ct);

            // Une fiche absente ou masquée n'apprend rien sur le jeton : la
            // traiter avant HandleAuthResponse évite qu'un 404 fasse passer pour
            // valide un jeton révoqué.
            if (res.StatusCode == System.Net.HttpStatusCode.NotFound) return null;

            HandleAuthResponse(res);
            if (!res.IsSuccessStatusCode) return null;

            return await res.Content.ReadFromJsonAsync<RpProfileDto>(JsonOptions, ct);
        }
        catch { return null; }
    }

    // ─── Amis RP ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Personnages à qui l'on a ouvert sa fiche, ou <c>null</c> si la requête a
    /// échoué. La distinction compte : une liste vide viderait l'écran et ferait
    /// croire qu'on n'a plus d'amis.
    /// </summary>
    public async Task<List<RpFriendDto>?> GetRpFriendsAsync(CancellationToken ct = default)
    {
        try
        {
            var res = await _http.GetAsync("api/rp-friends", ct);
            HandleAuthResponse(res);
            if (!res.IsSuccessStatusCode) return null;

            return await res.Content.ReadFromJsonAsync<List<RpFriendDto>>(JsonOptions, ct) ?? [];
        }
        catch { return null; }
    }

    /// <summary>Motif d'échec d'un ajout d'ami, pour dire lequel à l'utilisateur.</summary>
    public enum AddFriendResult { Added, NotFound, LimitReached, NoCharacterToken, Failed }

    /// <summary>
    /// Ouvre sa fiche à un personnage. Idempotent côté serveur.
    ///
    /// Le motif d'échec est distingué : « ce personnage n'a pas de fiche visible
    /// en jeu », « votre liste est pleine » et « liez un personnage » n'appellent
    /// pas la même réaction, et un message unique les rendait tous incompréhensibles.
    /// </summary>
    public async Task<AddFriendResult> AddRpFriendAsync(AddRpFriendRequest req,
                                                        CancellationToken ct = default)
    {
        try
        {
            var res = await _http.PostAsJsonAsync("api/rp-friends", req, JsonOptions, ct);
            HandleAuthResponse(res);

            if (res.IsSuccessStatusCode) return AddFriendResult.Added;

            return res.StatusCode switch
            {
                System.Net.HttpStatusCode.NotFound  => AddFriendResult.NotFound,
                System.Net.HttpStatusCode.Conflict  => await IsLimitAsync(res, ct),
                _                                   => AddFriendResult.Failed,
            };
        }
        catch { return AddFriendResult.Failed; }
    }

    /// <summary>
    /// Deux refus partagent le code 409 : la liste pleine et le jeton qui ne
    /// désigne aucun personnage. Seul le corps les sépare.
    /// </summary>
    private static async Task<AddFriendResult> IsLimitAsync(HttpResponseMessage res,
                                                            CancellationToken ct)
    {
        try
        {
            var body = await res.Content.ReadAsStringAsync(ct);
            return body.Contains("friend_limit", StringComparison.Ordinal)
                ? AddFriendResult.LimitReached
                : AddFriendResult.NoCharacterToken;
        }
        catch { return AddFriendResult.Failed; }
    }

    /// <summary>Retire l'accès accordé à un personnage. Idempotent.</summary>
    public async Task<bool> RemoveRpFriendAsync(string characterId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return false;

        try
        {
            var res = await _http.DeleteAsync(
                $"api/rp-friends/{Uri.EscapeDataString(characterId)}", ct);
            HandleAuthResponse(res);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    /// <summary>Aide-mémoire privé attaché à un ami.</summary>
    public async Task<bool> SetRpFriendNoteAsync(string characterId, string? note,
                                                 CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(characterId)) return false;

        try
        {
            var res = await _http.PatchAsJsonAsync(
                $"api/rp-friends/{Uri.EscapeDataString(characterId)}",
                new { note }, JsonOptions, ct);
            HandleAuthResponse(res);
            return res.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    // ─── RP Availability ─────────────────────────────────────────────────────

    /// <summary>
    /// Disponibilités publiques, ou <c>null</c> si la requête a échoué. Une liste
    /// vide et un échec réseau ne veulent pas dire la même chose : confondre les
    /// deux vidait la page « Autour de moi » au moindre hoquet de connexion, et
    /// ferait maintenant croire au plugin qu'il n'est plus déclaré disponible.
    /// </summary>
    public async Task<List<RpAvailabilityEntryDto>?> GetRpAvailabilitiesAsync(CancellationToken ct = default)
    {
        try
        {
            return await _publicHttp.GetFromJsonAsync<List<RpAvailabilityEntryDto>>(
                       "api/rp-availability", JsonOptions, ct)
                   ?? [];
        }
        catch { return null; }
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
