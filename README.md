# Eorzea Events — Plugin Dalamud

> 🇫🇷 [Français](#français) · 🇬🇧 [English](#english)

---

## Français

Plugin Dalamud pour [eorzea.events](https://eorzea.events) — gérez vos sessions de RP ouvert et consultez les événements directement depuis Final Fantasy XIV.

### Fonctionnalités

- 🎭 **RP Ouvert** — annoncez et gérez une session RP ouverte sans quitter le jeu (zone, serveur et position auto-remplis)
- 🟢 **Disponibilité & profil RP** — signalez que vous êtes ouvert au RP, renseignez votre profil (niveau, langues, approche…) et repérez les joueurs disponibles autour de vous
- 📅 **Événements** — consultez les événements à venir sur les 14 prochains jours et soyez prévenu au démarrage de chacun
- 🏠 **Lieux** — recherchez les établissements RP par nom, serveur ou quartier
- 🔔 **Notifications** — alerte écran native, bulle Dalamud ou message chat quand une session RP démarre près de vous (filtrable par monde et par langue)
- 📊 **Barre d'info serveur (DTR)** — indicateurs RP, événements et disponibilité directement dans la barre d'info de Dalamud
- 👥 **Multi-personnages** — un token distinct par personnage, couplage automatique via le navigateur
- 🌐 **Bilingue** — interface disponible en français et en anglais (détection automatique depuis le client FFXIV)

### Installation

1. Ouvrez **XIVLauncher** → **Paramètres Dalamud** → onglet **Expérimental**
2. Dans "Dépôts de plugins personnalisés", ajoutez :
   ```
   https://raw.githubusercontent.com/Neprena/eorzea-events-plugin/main/repo.json
   ```
3. Enregistrez, ouvrez le **Gestionnaire de plugins** et cherchez **Eorzea Events**
4. Installez le plugin

### Première configuration

Au premier lancement, un assistant de couplage s'ouvre automatiquement :

1. Connectez-vous in-game sur le personnage à lier
2. Le plugin lit son nom et son monde via Dalamud, puis ouvre une page de confirmation dans votre navigateur
3. Cliquez sur **Confirmer** : le couplage se fait automatiquement, aucun token à copier-coller

Chaque personnage dispose de son propre token. Pour en lier un nouveau, connectez-vous dessus puis lancez `/eorzea link`.

> Vous pouvez rouvrir l'assistant à tout moment via `/eorzea config`.

### Commandes

| Commande | Action |
|---|---|
| `/eorzea` | Ouvre le panneau principal |
| `/eorzea config` | Ouvre les paramètres |
| `/eorzea link` | Lie le personnage actuellement connecté |

### Paramètres disponibles

- **Notifications RP** : alerte écran native FFXIV, bulle Dalamud, message dans le chat ; filtres « mon monde uniquement » et par langue de RP
- **Alertes de session** : proposition de démarrage au tag RP, avertissement en cas de changement de zone, de retrait du tag ou de session bientôt expirée
- **Événements** : notification au démarrage d'un événement (bulle Dalamud et/ou chat)
- **Barre d'info serveur (DTR)** : afficher ou masquer les indicateurs RP, événements et disponibilité
- **Disponibilité & profil RP** : activer l'indicateur de disponibilité, configurer le profil RP, demander l'activation à la connexion
- **Langue** : automatique, français ou anglais

---

## English

Dalamud plugin for [eorzea.events](https://eorzea.events) — manage your open RP sessions and browse upcoming events directly from Final Fantasy XIV.

### Features

- 🎭 **Open RP** — announce and manage an open RP session without leaving the game (zone, server and position auto-filled)
- 🟢 **RP availability & profile** — flag yourself as open to RP, fill in your profile (level, languages, approach…) and spot available players around you
- 📅 **Events** — browse events scheduled in the next 14 days and get notified when each one starts
- 🏠 **Venues** — search RP establishments by name, server or ward
- 🔔 **Notifications** — native screen alert, Dalamud bubble or chat message when an RP session starts near you (filterable by world and language)
- 📊 **Server Info Bar (DTR)** — RP, events and availability indicators right in Dalamud's info bar
- 👥 **Multi-character** — a separate token per character, with automatic browser-based linking
- 🌐 **Bilingual** — interface available in French and English (auto-detected from your FFXIV client language)

### Installation

1. Open **XIVLauncher** → **Dalamud Settings** → **Experimental** tab
2. Under "Custom Plugin Repositories", add:
   ```
   https://raw.githubusercontent.com/Neprena/eorzea-events-plugin/main/repo.json
   ```
3. Save, open the **Plugin Installer** and search for **Eorzea Events**
4. Install the plugin

### First-time setup

A linking wizard opens automatically on first launch:

1. Log in on the character you want to link
2. The plugin reads its name and world through Dalamud, then opens a confirmation page in your browser
3. Click **Confirm**: linking happens automatically, no token to copy and paste

Each character has its own token. To link a new one, log in on it then run `/eorzea link`.

> You can reopen the wizard at any time with `/eorzea config`.

### Commands

| Command | Action |
|---|---|
| `/eorzea` | Open the main panel |
| `/eorzea config` | Open settings |
| `/eorzea link` | Link the currently logged-in character |

### Available settings

- **RP notifications**: native FFXIV screen alert, Dalamud bubble, chat message; "my world only" and RP-language filters
- **Session alerts**: suggest session on RP tag activation, warn on zone change, tag removal or session about to expire
- **Events**: notification when an event starts (Dalamud bubble and/or chat)
- **Server Info Bar (DTR)**: show or hide the RP, events and availability indicators
- **RP availability & profile**: toggle the availability indicator, set up your RP profile, prompt on login
- **Language**: auto, French or English
