# Migration Dalamud API 14 → API 15

Patch FFXIV 7.5 sorti le 29 avril 2026. **Le SDK `Dalamud.NET.Sdk v15.0.0` est disponible sur NuGet — la migration peut commencer.**

Source officielle : https://dalamud.dev/versions/v15/

---

## Fichiers à modifier

| Fichier | Action |
|---|---|
| `EorzeaEventsPlugin/EorzeaEventsPlugin.csproj` | `Sdk="Dalamud.NET.Sdk/15.0.0"` |
| `repo.json` | `"DalamudApiLevel": 15` |
| `EorzeaEventsPlugin/Plugin.cs` | Vérifier `OnTerritoryChanged` — `ZoneInitEventArgs` utilise des `RowRef` |
| `EorzeaEventsPlugin/Windows/*.cs` | Auditer usage ImRaii (`IEndObjects` → `ref struct`) — compiler pour voir les erreurs |
| `EorzeaEventsPlugin/LocationDebugSnapshot.cs` | Vérifier `HousingManager` (FFXIVClientStructs) — breaking changes CS à confirmer |
| `EorzeaEventsPlugin/EorzeaEventsPlugin.json` | S'assurer que le manifeste dans le zip est exact (n'est plus écrasé par Dalamud) |

---

## Analyse d'impact sur ce plugin

### IClientState.LocalPlayer supprimé — Pas d'impact ✅
Le plugin utilise exclusivement `ObjectTable.LocalPlayer` (pas `ClientState.LocalPlayer`).
Usages : `Plugin.cs:302`, `Plugin.cs:344`, `Plugin.cs:353`, `LocationDebugSnapshot.cs:47`, `MapHelper.cs:43`, `MySessionWindow.cs:58,76,80`, `MainWindow.cs:270`.

### IClientState.TerritoryChanged — Ajustement mineur ⚠️
`ZoneInitEventArgs` utilise désormais des `RowRef` à la place des `uint` bruts.
Vérifier et adapter la signature du handler `OnTerritoryChanged` dans `Plugin.cs`.

### ImRaii IEndObjects → ref struct — Faible impact ⚠️
`IEndObjects` supprimés au profit de `ref struct` pour réduire les allocations GC.
Si les fenêtres utilisent `using var`, généralement aucun changement. Laisser le compilateur signaler les erreurs.

### IChatGui — Pas d'impact ✅
La refonte porte sur les événements de *réception* de chat. Ce plugin ne fait qu'écrire (`ChatGui.Print` + `SeString`). Aucune action requise.

### Énumérations (ObjectKind, etc.) — Pas d'impact ✅
`ObjectKind` et autres enums synchronisées avec FFXIVClientStructs. Non utilisées directement dans ce plugin.

### HousingManager (FFXIVClientStructs) — Inconnu ⚠️
`LocationDebugSnapshot.cs` utilise `HousingManager` en code unsafe.
Laisser le compilateur signaler les erreurs après le bump SDK.

### IGameGui — Amélioration optionnelle ℹ️
Nouvelle surcharge `OpenMapWithMapLink(uint territory, uint map, Vector3 worldPos)` disponible.
Remplace la construction via `MapLinkPayload`. Migration optionnelle.

### Manifeste distribué ⚠️
Dalamud n'écrase plus le `EorzeaEventsPlugin.json` du zip. S'assurer que le fichier est complet.

---

## Procédure de migration

1. Bumper `Dalamud.NET.Sdk` → `15.0.0` dans le `.csproj`
2. Mettre `"DalamudApiLevel": 15` dans `repo.json`
3. Builder — laisser le compilateur lister les erreurs
4. Corriger `OnTerritoryChanged` si la signature a changé
5. Corriger tout ce que le compilateur signale (ImRaii, CS, etc.)
6. Vérifier `EorzeaEventsPlugin.json` dans le dossier de sortie

---

## Références

- [What's New in Dalamud v15](https://dalamud.dev/versions/v15/)
- [Dalamud branche api15](https://github.com/goatcorp/Dalamud/tree/api15)
- [Dalamud.NET.Sdk sur NuGet](https://www.nuget.org/packages/Dalamud.NET.Sdk/)
- [FFXIVClientStructs](https://github.com/aers/FFXIVClientStructs)
