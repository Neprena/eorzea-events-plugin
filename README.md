# Plugin Dalamud — Eorzea Events

Plugin Dalamud pour interagir avec [eorzea.events](https://eorzea.events) directement depuis FFXIV.

## Fonctionnalités

- 🎭 **RP Live** : créer et terminer une session RP sauvage depuis le jeu, avec zone et serveur auto-remplis
- 🔄 Restauration automatique de la session si le jeu est relancé

---

## Installation (via repo custom Dalamud)

1. Ouvrez **XIVLauncher** → **Dalamud Settings** → onglet **Experimental**
2. Dans "Custom Plugin Repositories", ajoutez :
   ```
   https://raw.githubusercontent.com/Neprena/eorzea-events-plugin/main/repo.json
   ```
3. Enregistrez, puis ouvrez le **Plugin Installer** et cherchez **Eorzea Events**
4. Installez et redémarrez Dalamud si demandé

### Configuration

1. Dans FFXIV, tapez `/eorzea config` ou cliquez sur l'icône du plugin
2. Générez un **token API** sur [eorzea.events/dashboard](https://eorzea.events/dashboard)
3. Collez-le dans le champ Token API et enregistrez

### Utilisation

- `/eorzea` — ouvre le panneau principal
- `/eorzea config` — ouvre la configuration

Dans le panneau, remplissez le titre de votre session (le serveur et la zone sont auto-remplis depuis votre position en jeu), puis cliquez sur **Démarrer la session RP**.

---

## Développement

### Prérequis

- [XIVLauncher](https://github.com/goatcorp/FFXIVQuickLauncher) avec Dalamud activé
- .NET SDK (version compatible avec le Sdk `Dalamud.NET.Sdk` utilisé)

### Build local

```bash
cd EorzeaEventsPlugin
dotnet build -c Debug
```

Le `.dll` compilé se trouve dans `bin/Debug/`.

Copiez le dossier dans `%APPDATA%\XIVLauncher\devPlugins\EorzeaEventsPlugin\`.

### Release

Les releases sont gérées automatiquement via GitHub Actions.

Créez un tag `vX.Y.Z` pour déclencher un build Release, packager le zip et créer la GitHub Release :

```bash
git tag v1.2.0
git push origin v1.2.0
```

Le workflow :
1. Build en mode Release avec la version du tag
2. Package `EorzeaEventsPlugin.zip` (DLL + JSON + banner)
3. Met à jour `AssemblyVersion` dans `repo.json` et commit sur `main`
4. Crée la GitHub Release avec le zip en pièce jointe

---

## Structure

```
EorzeaEventsPlugin/
├── Plugin.cs              # Point d'entrée Dalamud
├── Configuration.cs       # Persistance des paramètres
├── Api/
│   └── ApiClient.cs       # Client HTTP vers l'API Eorzea Events
└── Windows/
    ├── MainWindow.cs      # Fenêtre principale (RP Live)
    ├── ConfigWindow.cs    # Fenêtre de configuration
    └── SetupWindow.cs     # Assistant de première configuration
```
