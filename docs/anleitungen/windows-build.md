# Eco Cards – Windows-Build erstellen

Diese Anleitung erzeugt eine startbare Windows-Version aus dem Quellprojekt. Voraussetzung ist die abgeschlossene [Einrichtung in Godot](installation-godot.md). Ein erfolgreicher `dotnet build` allein erzeugt noch keinen vollständigen Spielordner für die Weitergabe.

## 1. Projekt vor dem Export prüfen

1. Öffne das Projekt in **Godot 4.6.3 .NET** und speichere deine Änderungen.
2. Warte auf den Abschluss der Assetimporte. Fehlende Modelle, Texturen oder Szenen müssen vor der Weitergabe geklärt werden.
3. Kompiliere C# über den **Build-/Hammer-Button** oder im Projektordner:

   ```powershell
   dotnet build .\GameDesign-Prototyp.sln
   ```

4. Starte mit **F5** und prüfe Hauptmenü, Partiestart, eine Kartenplatzierung und einen Rundenwechsel.
5. Stoppe den Lauf mit **F8**.

Behebe konkrete Build- und Importfehler, bevor du exportierst. Ein Export kann fehlende Quelldateien nicht wiederherstellen.

## 2. Passende Exportvorlagen installieren

Öffne **Editor → Exportvorlagen verwalten** und installiere die Vorlagen für **Godot 4.6.3 .NET**. Bei manueller Installation findest du sie im [Godot-Archiv](https://godotengine.org/download/archive/4.6.3-stable/) unter **Export templates – .NET**.

Editor und Vorlagen müssen in Versionsnummer und .NET-Variante übereinstimmen.

## 3. Windows-Preset einstellen

Öffne **Projekt → Exportieren** und wähle **Windows Desktop**. Fehlt der Eintrag, lege ihn über **Hinzufügen → Windows Desktop** an.

| Einstellung | Für diese Ausgabe |
| --- | --- |
| Zielplattform | Windows Desktop |
| Architektur | `x86_64` für Windows 64-Bit auf Intel-/AMD-Rechnern |
| Ausgabepfad | Neuer Ausgabeordner außerhalb des Projektordners |
| Dateiname | Beispielsweise `EcoCards.exe` |
| Mit Debug exportieren | Für die Weitergabe ausschalten |
| Ressourcen | Benötigte Spielszenen und Assets aufnehmen; lokale Arbeitsordner ausschließen |

Das vorhandene Preset in `export_presets.cfg` verwendet `x86_64`, exportiert alle Ressourcen und bettet die PCK nicht in die EXE ein. Deshalb gehört die erzeugte PCK zur Ausgabe. Godots Windows-Export verpackt die Spielressourcen zusammen mit einer ausführbaren Laufzeit. [Godot-Dokumentation zum Windows-Export](https://docs.godotengine.org/en/4.6/tutorials/export/exporting_for_windows.html)

### Lokale Zusatzinhalte ausschließen

Ein Eintrag in `.gitignore` verhindert nicht automatisch die Aufnahme in einen Godot-Export. Prüfe unter **Ressourcen** den Ausschlussfilter. Für die Waldversion ohne lokale zweite Karte und Arbeitsmaterial kannst du diese Ordner ausschließen:

```text
level_two/*, tmp/*, output/*
```

Verwende diesen Filter nur für eine Ausgabe, die keine Inhalte aus diesen Ordnern benötigt. Der aktuelle Ausschlussfilter ist leer; die Angabe oben ist eine empfohlene Exporteinstellung, keine bereits gesetzte Vorgabe.

Ein Ausgabeordner außerhalb des Projekts verhindert außerdem, dass neue Exportdateien beim nächsten Assetimport wieder als Projektinhalt auftauchen.

## 4. Startbare Version exportieren

1. Klicke **Projekt exportieren**.
2. Wähle den vorbereiteten Ausgabeordner und den EXE-Dateinamen.
3. Deaktiviere **Mit Debug exportieren**, wenn du die Version weitergeben möchtest.
4. Warte, bis der Export abgeschlossen ist, und prüfe die Exportausgabe auf Fehler.

**PCK/ZIP exportieren** erzeugt lediglich ein Ressourcenpaket. Für eine startbare Windows-Version brauchst du **Projekt exportieren**.

Die Ausgabe besteht aus der EXE, bei dieser Einstellung einer separaten PCK und den erzeugten .NET-Dateien beziehungsweise Unterordnern. Dateinamen können vom Projekt- und Exportnamen abhängen. Lasse den vollständigen erzeugten Ordner zusammen, statt nur die EXE zu kopieren.

## 5. Export außerhalb des Editors testen

Starte die exportierte EXE direkt aus ihrem Ausgabeordner. Prüfe dabei:

- Hauptmenü und Start einer Waldpartie;
- Kartenanzeige, gültige Platzierung und Rücknahme mit der Schaufel im selben Zug;
- Rundenabschluss mit Kalender, Wasserabrechnung und Handauffüllung;
- Pausenmenü, Lexikon, Einstellungen und Rückkehr ins Spiel;
- hörbare Spielgeräusche sowie das Beenden und erneute Starten der EXE.

Beim ersten Profilstart führt das Tutorial durch die anfänglichen Freigaben für Karten und Aktionen.

Diese Prüfung muss mit der exportierten Version erfolgen. Ein erfolgreicher Lauf im Editor bestätigt nicht, dass alle benötigten Dateien im Ausgabeordner enthalten sind.

## 6. Ausgabe weitergeben

1. Verpacke den **gesamten Ausgabeordner** als ZIP.
2. Entpacke die ZIP probeweise an einem anderen Ort und starte die enthaltene EXE.
3. Gib die ZIP mit der [Spielanleitung](spielanleitung.md) weiter. Wenn möglich, prüfe sie zusätzlich auf einem zweiten Windows-Rechner.

Der Empfänger muss die ZIP vollständig entpacken. Godot, Blender und das .NET SDK gehören zur Entwicklungsumgebung und müssen zum Spielen einer vollständig exportierten Ausgabe nicht installiert werden.
