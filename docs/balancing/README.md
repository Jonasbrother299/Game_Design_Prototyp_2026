# Balancing bearbeiten

## Zentraler Einstieg

Öffne in Godot:

`res://data/balance/game_balance.tres`

Diese Resource steuert die globalen Spielregeln und verweist auf alle
Pflanzen- und Ereignis-Resources. `BoardManager`, `TurnManager`,
`PlantDatabase` und `EventDatabase` verwenden dieselbe Resource.

Tutorialwerte gehören nicht zu diesem Balancing-System.

## Globale Werte

`game_balance.tres` enthält:

- Startwasser sowie Sieg- und Niederlagegrenze
- Start- und Maximalgröße der Kartenhand
- Kartenlimit pro Runde und Freischaltung des Handabwurfs
- Intervall und Mindestnenner der Pflanzenausbreitung
- allgemeine Ereignischance
- Mindestgröße einer Monokultur
- Sperrdauer abgestorbener Pflanzen
- Boardform, Spalten, Reihen und Radius

## Pflanzenwerte

Die Gruppe `Pflanzen` verweist auf die Resources unter `data/plants/`.
Wähle eine verknüpfte Resource aus, um diese Werte zu ändern:

- Startkartenanzahl und Ziehgewicht
- Wasserverbrauch und Wasserproduktion
- Wachstumsrunden und Wachstumsstufen
- Ausbreitungsnenner
- Ereignisresistenz pro Wachstumsstufe
- Pilz- und Blumenstärke
- erlaubte Lichtstufen

Ein Ziehgewicht von `0` schließt eine Pflanze aus zufällig gezogenen Karten
aus. Höhere Werte erhöhen ihren Anteil relativ zu den anderen Pflanzen.

## Ereigniswerte

Die Gruppe `Ereignisse` verweist auf die Resources unter `data/events/`.
Eine Ereignis-Resource enthält:

- Auswahlgewicht
- Wassermodifikator und Dauer
- Windstärke
- Sterbewahrscheinlichkeiten und Bedingungen

Ein Auswahlgewicht von `0` verhindert die zufällige Auswahl. Die allgemeine
Ereignischance in `game_balance.tres` bestimmt weiterhin, ob überhaupt ein
Ereignis beginnt.

## Wahrscheinlichkeiten

Ein Nenner `N` bedeutet eine Wahrscheinlichkeit von `1/N`.

Beispiele:

- `EventChanceDenominator = 3`: Ereignischance `1/3`
- `SpreadChanceDenominator = 6`: Ausbreitungschance `1/6`
- Sterbenenner `4`: Sterbewahrscheinlichkeit `1/4`

Größere Nenner senken die Wahrscheinlichkeit.

## Neue Balancing-Werte

Ein neuer globaler Wert gehört in `GameConfig` und
`game_balance.tres`. Ein neuer pflanzen- oder ereignisspezifischer Wert gehört
in `PlantDefinition` beziehungsweise `EventDefinition`.

Die zentrale Resource muss für neue Zahlen einer bereits verknüpften Pflanze
oder eines bereits verknüpften Ereignisses nicht erweitert werden.

Kamera, Darstellung, Animation und UI-Abstände bleiben in ihren jeweiligen
Szenen oder Komponenten. Diese Werte verändern keine Gameplay-Regeln.
