# Eorzea Events — Plugin Dalamud

> 🇫🇷 [Français](#français) · 🇬🇧 [English](#english)

---

## Français

Plugin Dalamud pour [eorzea.events](https://eorzea.events) — gérez vos sessions de RP ouvert et consultez les événements directement depuis Final Fantasy XIV.

### Fonctionnalités

- 🎭 **RP Ouvert** — annoncez et gérez une session RP ouverte sans quitter le jeu (zone, serveur et position auto-remplis)
- 📅 **Événements** — consultez les événements à venir sur les 14 prochains jours
- 🏠 **Lieux** — recherchez les établissements RP par nom, serveur ou quartier
- 🔔 **Notifications** — soyez alerté quand une nouvelle session RP démarre près de vous
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

Au premier lancement, un assistant s'ouvre automatiquement :

1. Rendez-vous sur [eorzea.events/dashboard](https://eorzea.events/dashboard)
2. Générez un **token API** dans votre espace personnel
3. Collez-le dans le champ dédié et enregistrez

> Vous pouvez aussi ouvrir l'assistant à tout moment via `/eorzea config`.

### Commandes

| Commande | Action |
|---|---|
| `/eorzea` | Ouvre le panneau principal |
| `/eorzea config` | Ouvre les paramètres |

### Paramètres disponibles

- **Notifications** : alerte écran native FFXIV, bulle Dalamud, message dans le chat
- **Alertes de session** : proposition de démarrage au tag RP, avertissement en cas de changement de zone ou de retrait du tag
- **Langue** : automatique, français ou anglais

---

## English

Dalamud plugin for [eorzea.events](https://eorzea.events) — manage your open RP sessions and browse upcoming events directly from Final Fantasy XIV.

### Features

- 🎭 **Open RP** — announce and manage an open RP session without leaving the game (zone, server and position auto-filled)
- 📅 **Events** — browse events scheduled in the next 14 days
- 🏠 **Venues** — search RP establishments by name, server or ward
- 🔔 **Notifications** — get alerted when a new RP session starts near you
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

A setup wizard opens automatically on first launch:

1. Go to [eorzea.events/dashboard](https://eorzea.events/dashboard)
2. Generate an **API token** from your personal dashboard
3. Paste it into the token field and save

> You can reopen the wizard at any time with `/eorzea config`.

### Commands

| Command | Action |
|---|---|
| `/eorzea` | Open the main panel |
| `/eorzea config` | Open settings |

### Available settings

- **Notifications**: native FFXIV screen alert, Dalamud bubble, chat message
- **Session alerts**: suggest session on RP tag activation, warn on zone change or tag removal
- **Language**: auto, French or English
