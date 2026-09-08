# Eco Cards

Eco Cards ist ein rundenbasiertes Kartenspiel mit einer 3D-Spielwelt in Godot.
Du platzierst Pflanzen auf einem Hexfeld, entwickelst ein Ökosystem und hältst
die alte Eiche am Leben. Dafür müssen Wasserproduktion, Wasserverbrauch und
Lichtbedingungen zusammenpassen.

Das Projekt befindet sich in Entwicklung.

## Spielprinzip

- Spiele Moos, Blumen, Pilze und Birken als Karten auf passende Felder.
- Pflanzen wachsen über mehrere Runden und können sich auf benachbarte Felder
  ausbreiten.
- Bäume spenden Schatten. Blumen fördern die Ausbreitung; Pilze können die
  Wasserproduktion benachbarter Pflanzen erhöhen.
- Regen, Starkregen, Wind, Hitze, Dürre und Schädlinge beeinflussen das Ökosystem.
- Beobachte die Wasseranzeige: Sinkt der Wasserstand auf null, stirbt die Eiche.

Das Spiel enthält ein Tutorial, ein Lexikon, Erfolge sowie Einstellungen für
Audio, Anzeige und Steuerung.

## Projekt starten

Voraussetzungen:

- Godot 4.6.3 in der .NET-Version für C#
- .NET 8 SDK
- Grafikunterstützung für den im Projekt verwendeten Forward+-Renderer

1. Das Repository klonen oder herunterladen.
2. `project.godot` im Godot-Projektmanager importieren und öffnen.
3. Den Import der Assets abwarten.
4. Das C#-Projekt über Godot oder im Projektordner über das Terminal kompilieren:

   ```powershell
   dotnet build GameDesign-Prototyp.sln
   ```

5. Mit **F5** das Projekt starten. Die Startszene ist
   [`scenes/UI/MainMenu.tscn`](scenes/UI/MainMenu.tscn).

Die Projektkonfiguration verwendet `Godot.NET.Sdk/4.6.3` und für Desktop
`net8.0`. Der Build-Befehl kompiliert C#; er prüft keinen Spielablauf.

## Steuerung

| Aktion | Bedienung |
| --- | --- |
| Karte platzieren | Karte mit der linken Maustaste auf ein gültiges Feld ziehen |
| Kamera drehen | Freie Fläche mit der linken Maustaste ziehen |
| Kamera zoomen | Mausrad |
| Nächste Runde | Button „Nächster Tag“ |
| Pausenmenü | Escape |

Weitere Optionen stehen unter **Einstellungen → Steuerung**.

## Projektstruktur

| Ordner | Inhalt |
| --- | --- |
| `assets/` | Modelle, Texturen, Kartenbilder, Schriften und Audio |
| `scenes/` | Spielwelt, Pflanzen, Menüs und Effekte |
| `scripts/` | C#-Code für Spielregeln, Darstellung und Bedienung |
| `data/` | Balancing sowie Pflanzen- und Ereigniswerte |
| `shaders/` | Shader für Wasser, Vegetation und weitere Effekte |
| `docs/` | Projektbezogene Dokumentation |

## Balancing und Tutorial

Die zentrale Balancing-Resource ist
[`data/balance/game_balance.tres`](data/balance/game_balance.tres). Sie verweist
auf die Pflanzen- und Ereigniswerte.

- [Balancing bearbeiten](docs/balancing/README.md)
- [Tutorial-Arbeitsbereich](docs/tutorial/README.md)
- [Geplanter Tutorialablauf](docs/tutorial/tutorial-flow.md)

Tutorialinhalte liegen in `scenes/tutorial/` und `scripts/tutorial/`. Der geplante
Ablauf beschreibt das Entwicklungsziel und ist nicht vollständig umgesetzt.
