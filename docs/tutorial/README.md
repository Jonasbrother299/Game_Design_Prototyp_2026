# Tutorial-Arbeitsbereich

## Status

Die Tutorialdarstellung ist aus `Main.tscn` und `GameHub` herausgelöst. Der bestehende Tutorialprototyp bleibt als neunseitige, manuell navigierte Abfolge erhalten.

Die Inhalte und Interaktionen entsprechen noch nicht dem geplanten finalen Tutorial. Der aktuelle Stand dient als funktionsfähige Basis für die weitere Entwicklung.

## Relevante Dateien

- [`TutorialOverlay.tscn`](../../scenes/tutorial/TutorialOverlay.tscn): Layout und visuelle Tutorialelemente
- [`TutorialOverlay.cs`](../../scripts/tutorial/TutorialOverlay.cs): Schnittstelle zur Darstellung
- [`TutorialManager.cs`](../../scripts/tutorial/TutorialManager.cs): aktueller Ablauf und Highlights
- [`GameManager.cs`](../../scripts/game/GameManager.cs): erzeugt den TutorialManager und übergibt seine Abhängigkeiten
- [`Main.tscn`](../../scenes/Main.tscn): instanziiert nur noch das TutorialOverlay
- [Geplanter Tutorialablauf](tutorial-flow.md): fachliche Vorgabe für die neun Schritte
- [Gameplay-Ereignisse](gameplay-events.md): allgemeine Schnittstellen des TurnManagers für die Tutorialsteuerung

## Zuständigkeiten

### TutorialOverlay

Das Overlay stellt Inhalte dar und meldet Navigationseingaben. Es enthält keine Gameplay-Regeln.

Öffentliche Aufgaben:

- Overlay ein- und ausblenden
- Titel und Text setzen
- Kartenbild und Karteninformationen anzeigen
- Zurück-/Weiter-Zustand setzen
- Fortschrittspunkte anzeigen
- Weiter- und Zurück-Ereignisse melden

### TutorialManager

Der Manager steuert die Reihenfolge der Schritte. Er darf Gameplay-Systeme beobachten und über klar definierte Schnittstellen ansprechen.

Er soll keine eigene Wasser-, Wachstums-, Platzierungs- oder Ausbreitungslogik enthalten.

### GameManager

Der GameManager verbindet das Tutorial einmalig mit Board, Kartenhand und TurnManager. Weitere Tutorialinhalte gehören nicht in den GameManager.

Beim normalen Partiestart liest er den profilspezifischen Tutorialstatus. Ein
Profil ohne bereits gestartetes Tutorial erhält den TutorialManager automatisch.
Nach einem erfolgreichen Start speichert er den Status. Spätere Partien starten
ohne Tutorial.

Das Pausenmenü kann für genau den nächsten Szenenstart eine Wiederholung
anfordern. Dafür beginnt eine neue Partie mit Tutorial; der gespeicherte
Profilstatus wird nicht zurückgesetzt.

### Main-Szene

`Main.tscn` instanziiert das Overlay. Einzelne Tutorialfenster, Texte und Pfeile sollen nicht wieder direkt in die Hauptszene eingefügt werden.

## Aktuelle Einschränkungen

- Der TutorialManager verwendet noch den alten, statischen Inhalt.
- Die Schritte warten nicht auf Gameplay-Aktionen.
- Die vier Rundenphasen laufen noch synchron ohne Pausen für Tutorialanimationen.
- Ein Spotlight, Pfeile, Tooltips und ein Eventbanner fehlen.
- `data/tutorial/` und ein eigenes Tutoriallevel sind noch nicht angelegt.

## Empfohlene nächste Arbeiten

1. Die Zielschritte aus [`tutorial-flow.md`](tutorial-flow.md) in benannte Schrittkennungen übertragen.
2. Den TutorialManager mit den [Gameplay-Ereignissen](gameplay-events.md) verbinden.
3. Spotlight, Pfeil und Tooltip als eigenständige Unterkomponenten des Overlays anlegen.
4. Den TutorialManager auf Aktionen warten lassen, statt alle Schritte nur über „Weiter“ zu schalten.
5. Ein Tutoriallevel mit vorbereiteter Hand, Zielkoordinaten, garantierter Ausbreitung und garantiertem Regen definieren.
6. Erst danach die finalen Texte, Animationen und Übergänge ausarbeiten.

## Arbeitsregel

Die Tutorial-Person arbeitet eigenständig in:

- `scenes/tutorial/`
- `scripts/tutorial/`
- `docs/tutorial/`
- `data/tutorial/`
- `assets/ui/tutorial/`

Diese gemeinsam genutzten Integrationsdateien werden nur nach Abstimmung geändert:

- `scenes/Main.tscn`
- `scripts/game/GameManager.cs`
- `scripts/UI/GameHub.cs`

Änderungen an Gameplay-Systemen benötigen eine kleine, allgemein nutzbare Schnittstelle. Die normalen Spielregeln dürfen keine Tutorial-Sonderlogik enthalten.

Mit dem aktuellen Stand kann die Tutorial-Person die Schrittfolge, Darstellung, Texte, Highlights und Tutorialdaten bearbeiten. Die allgemeinen Gameplay-Ereignisse für die spätere Anbindung sind vorhanden.
