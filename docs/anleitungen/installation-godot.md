# Eco Cards – Installation in Godot

Diese Anleitung richtet das Quellprojekt unter **Windows 64-Bit** ein. Du benötigst den vollständigen Projektordner einschließlich Szenen, Scripts, Daten und Assets.

Wenn du bereits eine fertige Windows-Spielversion erhalten hast, entpacke deren gesamten Ausgabeordner und starte die enthaltene EXE. Für diese Version brauchst du keinen Godot-Editor, kein Blender und kein .NET SDK. Lasse die mitgelieferten Dateien neben der EXE zusammen; das Spiel verwendet seine exportierten Ressourcen und Laufzeitdateien.

## 1. Entwicklungsprogramme installieren

| Programm | Benötigte Variante | Zweck im Projekt |
| --- | --- | --- |
| [Godot 4.6.3](https://godotengine.org/download/archive/4.6.3-stable/) | **.NET, Windows x86_64** | Öffnet das Projekt und unterstützt seine C#-Scripts |
| [.NET 8 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) | **SDK, Windows x64** | Kompiliert den C#-Code für das Desktop-Ziel `net8.0` |
| [Blender](https://www.blender.org/download/) | Windows-Version | Ermöglicht den Import vorhandener `.blend`-Modelle |

Entpacke das gesamte Godot-Archiv. Der Ordner `GodotSharp` muss bei der Godot-EXE bleiben. Die normale Godot-Ausgabe ohne .NET reicht für dieses Projekt nicht aus. Das SDK wird zusätzlich benötigt, weil es die Werkzeuge zum Kompilieren enthält. [Godot-Dokumentation zu C#](https://docs.godotengine.org/en/4.6/tutorials/scripting/c_sharp/c_sharp_basics.html)

Öffne nach der SDK-Installation eine neue PowerShell und prüfe:

```powershell
dotnet --list-sdks
```

In der Ausgabe muss das installierte SDK erscheinen. Die Projektdatei `GameDesign-Prototyp.csproj` verwendet `Godot.NET.Sdk/4.6.3` und für Desktop `net8.0`; diese Angaben bestimmen die Einrichtung.

## 2. Projektordner vorbereiten

1. Lade das vollständige Projekt herunter oder klone das Repository.
2. Entpacke ein heruntergeladenes Archiv vollständig. Öffne das Projekt nicht direkt aus der ZIP.
3. Prüfe den Ordner: `project.godot`, `GameDesign-Prototyp.sln`, `GameDesign-Prototyp.csproj`, `assets`, `scenes`, `scripts` und `data` müssen vorhanden sein.
4. Öffne PowerShell genau in diesem Ordner, beispielsweise über **Im Terminal öffnen** im Explorer.

Die lokale zweite Karte unter `level_two/` ist nicht im getrackten Repository enthalten. Sie ist kein Bestandteil der hier eingerichteten Waldkarte. Für lokale Zusatzszenen werden deren eigene Assets benötigt.

## 3. C# kompilieren

Führe im Projektordner aus:

```powershell
dotnet build .\GameDesign-Prototyp.sln
```

Beim ersten Build werden fehlende NuGet-Pakete heruntergeladen; dafür ist eine Internetverbindung nötig. Warte auf die Abschlussmeldung des erfolgreichen Builds.

Ein erfolgreicher Build bestätigt, dass C# kompiliert wurde. Er prüft noch nicht, ob alle Modelle importiert werden oder das Spiel läuft.

## 4. Projekt importieren und starten

1. Starte **Godot 4.6.3 .NET**.
2. Wähle im Projektmanager **Importieren** und öffne die `project.godot` aus dem Projektordner.
3. Öffne das importierte Projekt und warte, bis der Assetimport beendet ist. Der erste Import kann länger dauern als spätere Starts.
4. Prüfe die Ausgabe auf fehlende Ressourcen oder fehlgeschlagene Modellimporte.
5. Starte das gesamte Projekt mit **F5**. Falls eine Hauptszene ausgewählt werden soll, wähle `scenes/UI/MainMenu.tscn`.
6. Wähle im Hauptmenü **Spielen**. Das Tutorial erscheint beim ersten Start mit einem Profil. **F8** stoppt den Lauf aus dem Editor.

Godot ruft Blender für `.blend`-Importe auf. Den Installationspfad kannst du unter **Editor → Editoreinstellungen → Blender Path** festlegen. [Godot-Dokumentation zum Blender-Import](https://docs.godotengine.org/en/4.6/tutorials/assets_pipeline/importing_3d_scenes/available_formats.html#importing-blend-files-directly-within-godot)

## 5. Einrichtung prüfen

Prüfe nach dem ersten Start, ob das Hauptmenü erscheint, die Waldkarte geladen wird und du die erste erlaubte Tutorialkarte setzen kannst. Nach einem Rundenabschluss müssen Kalender und Wasseranzeige reagieren. So prüfst du auch die Verbindung zwischen importierten Assets, Oberfläche und Spielcode.

Spielregeln stehen in der [Spielanleitung](spielanleitung.md). Eine weitergebbare EXE erstellst du mit der Anleitung [Windows-Build](windows-build.md).
