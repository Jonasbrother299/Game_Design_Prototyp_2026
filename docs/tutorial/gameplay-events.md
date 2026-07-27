# Gameplay-Ereignisse für das Tutorial

Der `TurnManager` stellt allgemeine C#-Ereignisse bereit. Sie enthalten keine Tutoriallogik und können auch für HUD, Animationen und Effekte verwendet werden.

## Aktionen

- `TurnStarted(int round)`: Eine neue Runde wurde gestartet.
- `EndTurnRequested(int round)`: Das gültige Rundenende wurde ausgelöst.
- `PlantPlaced(PlantType plantType, HexCoord coord)`: Eine Handkarte wurde erfolgreich platziert.
- `EventActivated(GameEventType eventType)`: Ein Wetterereignis wurde aktiviert.

Der TutorialManager kann ein festes Wetterereignis mit `AddEvent(GameEventType)` aktivieren. Die Methode gibt `false` zurück, wenn kein laufendes Spiel oder keine passende Ereignisressource vorhanden ist.

## Rundenphasen

`EndTurn()` löst diese Phasen nacheinander aus:

1. `WaterPhaseResolved(WaterPhaseResult result)`
2. `SpreadPhaseResolved(SpreadPhaseResult result)`
3. `GrowthPhaseResolved(GrowthPhaseResult result)`
4. `EventPhaseResolved(EventPhaseResult result)`

### Wasser

`WaterPhaseResult` enthält den Wasserstand vor und nach der Phase, die Wettereinwirkung sowie Produktion und Verbrauch der Pflanzen.

`PlantWaterResult` liefert je belegtem Feld:

- Koordinate und Pflanzentyp
- Grundproduktion
- Verbrauch
- Nachbarschaftsbonus
- Nettoänderung

Damit kann die UI gezielte `+1`- und `-1`-Hinweise über Feldern darstellen.

### Ausbreitung

`SpreadPhaseResult.Spreads` enthält jede erfolgreiche Ausbreitung mit Pflanzentyp, Quellfeld und Zielfeld.

Neu verbreitete Pflanzen wachsen nicht sofort in derselben Runde.

### Wachstum

`GrowthPhaseResult.Plants` enthält nur Pflanzen, deren verbleibende Wachstumsrunden geändert wurden.

Jeder Eintrag enthält:

- Pflanzentyp und Koordinate
- vorherige und neue Restdauer
- `BecameMature` für den Übergang zur ausgewachsenen Pflanze

### Ereignis

`EventPhaseResult` enthält die nach der Runde weiterhin aktiven sowie die beendeten Wetterereignisse.

Die Eventphase wählt derzeit kein zufälliges neues Ereignis. Das Tutorial kann Regen gezielt über `AddEvent(GameEventType.Rain)` aktivieren.

## Verwendung

Die Ereignisse werden synchron innerhalb von `EndTurn()` ausgelöst. Der TutorialManager soll die Ergebnisdaten speichern und seine Darstellung anschließend abspielen.

Abonnements müssen beim Entfernen des TutorialManagers wieder getrennt werden. Dadurch entstehen nach einem Szenenwechsel keine doppelten Reaktionen.
