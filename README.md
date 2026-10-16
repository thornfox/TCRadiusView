# TC Radius View

**Author:** ThornFox · **Version:** 1.0.0 · **Framework:** Oxide / uMod (Rust) · **License:** MIT

> ✅ **Tested on the RustRentner server – [rustrentner.de](https://rustrentner.de)**
> ✅ **Getestet auf dem RustRentner-Server – [rustrentner.de](https://rustrentner.de)**

---

## Files / Dateien
```
TCRadiusView.cs              Plugin → oxide/plugins/
lang/en/TCRadiusView.json    English messages → oxide/lang/en/
lang/de/TCRadiusView.json    Deutsche Texte → oxide/lang/de/
CHANGELOG.md                 Changelog
LICENSE                      MIT License
```

---

## English

### Description
Toggle a visual outline of the building privilege zone of all your tool cupboards with a single chat command. The outline is drawn only for the player who activated it and stays visible until the same command is used again.

The zone is drawn as a box around all building blocks of the cupboard's building, extended by the privilege range (Rust default: 16 m). Building privilege is projected by the building parts, not by the cupboard itself, so this matches the in-game behaviour closely. The real zone has slightly rounded corners.

### Installation
1. Copy `TCRadiusView.cs` to `oxide/plugins/`.
2. Optional: copy `lang/de/TCRadiusView.json` to `oxide/lang/de/`.
3. Grant permissions (see below).

### Commands
| Command | Description |
|---|---|
| `/tc` | Show the zones of your tool cupboards. Type it again to hide them. |

The command name can be changed in the config.

### Permissions
| Permission | Description |
|---|---|
| `tcradiusview.use` | Use `/tc` for cupboards you are authorized on |
| `tcradiusview.admin` | Use `/tc` for **all** cupboards in range (admins only) |

Example: `oxide.grant group default tcradiusview.use`

### Configuration
```json
{
  "Chat commands": ["tc"],
  "Privilege zone extension in meters (Rust default: 16)": 16.0,
  "Max distance from player to cupboard in meters": 150.0,
  "Max cupboards drawn per refresh": 25,
  "Refresh interval in seconds": 2.0,
  "Outline color (hex)": "#2ECC71",
  "Label color (hex)": "#FFFFFF",
  "Show label above cupboard": true
}
```

### Technical note
Rust only renders `ddraw` debug drawings for admins. The plugin sets the admin flag for a fraction of a second while drawing and removes it right after (common approach in plugins such as RaidTracker). The flag does not grant console admin rights (auth level is untouched).

### Tested on
This plugin is tested and running on the RustRentner server: **[rustrentner.de](https://rustrentner.de)**

### Changelog
- **1.0.0** – Initial release (see [CHANGELOG.md](CHANGELOG.md))

### License
Released under the [MIT License](LICENSE). © 2026 ThornFox

---

## Deutsch

### Beschreibung
Blendet mit einem Chat-Befehl den Bauberechtigungs-Bereich aller deiner Werkzeugschränke als Umriss ein. Den Umriss sieht nur der Spieler, der ihn aktiviert hat, und er bleibt sichtbar, bis derselbe Befehl erneut eingegeben wird.

Der Bereich wird als Box um alle Bauteile des Gebäudes gezeichnet, erweitert um die Bauberechtigungs-Reichweite (Rust-Standard: 16 m). Die Bauberechtigung geht von den Bauteilen aus, nicht vom Schrank selbst – deshalb entspricht die Darstellung recht genau dem Verhalten im Spiel. Der echte Bereich hat leicht abgerundete Ecken.

### Installation
1. `TCRadiusView.cs` nach `oxide/plugins/` kopieren.
2. Optional: `lang/de/TCRadiusView.json` nach `oxide/lang/de/` kopieren.
3. Berechtigungen vergeben (siehe unten).

### Befehle
| Befehl | Beschreibung |
|---|---|
| `/tc` | Bereiche deiner Werkzeugschränke einblenden. Erneut eingeben zum Ausblenden. |

Der Befehlsname kann in der Config geändert werden.

### Berechtigungen
| Berechtigung | Beschreibung |
|---|---|
| `tcradiusview.use` | `/tc` für Schränke, auf denen man autorisiert ist |
| `tcradiusview.admin` | `/tc` für **alle** Schränke in Reichweite (nur Admins) |

Beispiel: `oxide.grant group default tcradiusview.use`

### Konfiguration
Siehe JSON oben im englischen Teil. Die Schlüssel sind bewusst englisch.

### Technischer Hinweis
Rust zeigt `ddraw`-Zeichnungen nur Admins an. Das Plugin setzt deshalb für einen Sekundenbruchteil das Admin-Flag und entfernt es sofort wieder (gängiges Vorgehen, z. B. bei RaidTracker). Der Auth-Level bleibt unverändert, Konsolen-Adminrechte entstehen dadurch nicht.

### Getestet auf
Dieses Plugin ist getestet und läuft auf dem RustRentner-Server: **[rustrentner.de](https://rustrentner.de)**

### Changelog
- **1.0.0** – Erste Version (siehe [CHANGELOG.md](CHANGELOG.md))

### Lizenz
Veröffentlicht unter der [MIT-Lizenz](LICENSE). © 2026 ThornFox
