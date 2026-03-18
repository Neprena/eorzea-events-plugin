# Workflow plugin — Dev & Prod

## Dev (tester localement avant release)

1. **Modifier le code** dans `EorzeaEventsPlugin/`

2. **Build debug**
   ```bash
   cd EorzeaEventsPlugin
   dotnet build -c Debug
   ```

3. **Copier dans Dalamud devPlugins**
   ```powershell
   $src = "bin\Debug"
   $dst = "$env:APPDATA\XIVLauncher\devPlugins\EorzeaEventsPlugin"
   New-Item -ItemType Directory -Force -Path $dst
   Copy-Item "$src\*" $dst -Force
   ```

4. Dans FFXIV : `/xldev` → **Plugin Test** → recharger le plugin (ou redémarrer Dalamud)

---

## Prod (publier une nouvelle version)

1. **Mettre à jour la version** dans `EorzeaEventsPlugin/EorzeaEventsPlugin.csproj` :
   ```xml
   <Version>1.2.0</Version>
   ```

2. **Committer les changements** dans `eorzea-events` :
   ```bash
   cd C:\Users\yann\Projects\eorzea-events
   git add plugin/
   git commit -m "feat(plugin): description du changement"
   git push
   ```

3. **Poser le tag** (même numéro que dans le csproj) :
   ```bash
   git tag plugin-v1.2.0
   git push origin plugin-v1.2.0
   ```

4. **Vérifier le workflow** sur GitHub → `eorzea-events` → Actions → **Plugin Release**
   - ✅ Build → Package → repo.json mis à jour → Release créée

5. **Vérifier le résultat** sur `Neprena/eorzea-events-plugin` :
   - `repo.json` contient la nouvelle `AssemblyVersion`
   - La release `v1.2.0` existe avec `EorzeaEventsPlugin.zip`

Les joueurs reçoivent la mise à jour automatiquement au prochain lancement de Dalamud.

---

## Résumé

| Action | Commande |
|--------|----------|
| Tester en local | `dotnet build -c Debug` + copie dans devPlugins |
| Publier | bump version dans `.csproj` → commit → `git tag plugin-vX.Y.Z` → push tag |
