using Dalamud.Interface.Textures.TextureWraps;
using System.Net.Http;

namespace EorzeaEventsPlugin.Ui;

/// <summary>
/// Images distantes : bannières d'établissement, affiches d'événement,
/// portraits de personnage.
///
/// Un seul <see cref="HttpClient"/> pour tout le plugin. Chaque fenêtre avait
/// le sien, ce qui multiplie les pools de connexions sans raison et finit par
/// épuiser les sockets, un classique du genre.
///
/// Le cache est borné : au-delà de <see cref="Capacity"/> entrées, les plus
/// anciennes sont libérées. L'ancien code vidait tout le cache à chaque
/// recherche d'établissement, ce qui obligeait à retélécharger les mêmes
/// bannières en boucle.
/// </summary>
internal static class Textures
{
    private const int Capacity = 48;

    private static readonly HttpClient Http = new() { Timeout = TimeSpan.FromSeconds(20) };

    /// <summary>Insertion ordonnée : la première clé est la plus ancienne.</summary>
    private static readonly Dictionary<string, Task<IDalamudTextureWrap?>> Cache = [];
    private static readonly List<string> Order = [];

    /// <summary>
    /// Texture correspondant à l'URL, ou <c>null</c> tant qu'elle n'est pas
    /// arrivée. Le téléchargement est lancé au premier appel et l'appelant se
    /// contente de redemander à la frame suivante.
    /// </summary>
    public static IDalamudTextureWrap? Get(string? url)
    {
        if (string.IsNullOrEmpty(url)) return null;

        if (!Cache.TryGetValue(url, out var task))
        {
            task = LoadAsync(url);
            Cache[url] = task;
            Order.Add(url);
            Trim();
        }

        return task.IsCompletedSuccessfully ? task.Result : null;
    }

    private static async Task<IDalamudTextureWrap?> LoadAsync(string url)
    {
        try
        {
            var bytes = await Http.GetByteArrayAsync(url);
            return await Plugin.TextureProvider.CreateFromImageAsync(
                new ReadOnlyMemory<byte>(bytes), debugName: url);
        }
        catch (Exception ex)
        {
            // Une bannière absente ou un lien mort ne doit pas remonter : la
            // carte s'affiche simplement sans image.
            Plugin.Log.Debug(ex, $"[EorzeaEvents] Image non chargée : {url}");
            return null;
        }
    }

    private static void Trim()
    {
        while (Order.Count > Capacity)
        {
            var oldest = Order[0];
            Order.RemoveAt(0);

            if (Cache.Remove(oldest, out var task) && task.IsCompletedSuccessfully)
                task.Result?.Dispose();
        }
    }

    /// <summary>
    /// Libère toutes les textures. Appelé au déchargement du plugin : sans
    /// cela, chaque rechargement laissait derrière lui les images téléchargées.
    /// </summary>
    public static void Dispose()
    {
        foreach (var (_, task) in Cache)
            if (task.IsCompletedSuccessfully) task.Result?.Dispose();

        Cache.Clear();
        Order.Clear();
        Http.Dispose();
    }
}
