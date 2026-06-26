# JK Synchro-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **JK Synchro-Strategie** ist ein StockSharp-Port des MetaTrader 5-Expertenberaters "JK synchro" (MQL-ID 2415). Der ursprüngliche Roboter zählt, wie viele der zuletzt abgeschlossenen Kerzen tiefer oder höher als die Eröffnung geschlossen haben, und öffnet dann eine Position in der dominierenden Richtung. Dieser Port repliziert das Verhalten und fügt stark typisierte Parameter, integrierte Risikomanagement-Hooks und umfangreiches Logging über StockSharp hinzu.

## Handelslogik

1. Abonnieren der durch `CandleType` definierten Kerzenquelle und Warten auf abgeschlossene Kerzen.
2. Pflegen eines gleitenden Fensters von `AnalysisPeriod` Kerzen. Für jede Kerze:
   - Erhöhen des **bärischen** Zählers wenn `Open > Close`.
   - Erhöhen des **bullischen** Zählers wenn `Open < Close`.
   - Ignorieren von Doji-Kerzen wenn `Open == Close`.
3. Sobald das Fenster gefüllt ist, die Dominanz prüfen:
   - Wenn bärische Kerzen die bullischen überwiegen, eine **Long**-Eröffnung vorbereiten.
   - Wenn bullische Kerzen die bärischen überwiegen, eine **Short**-Eröffnung vorbereiten.
4. Vor dem Einstieg in einen Trade überprüft die Strategie:
   - Die Strategie ist online und darf handeln (`IsFormedAndOnlineAndAllowTrading`).
   - Die aktuelle Stunde liegt zwischen `StartHour` und `EndHour` (einschließlich).
   - Die durch `PauseBetweenTradesSeconds` definierte Abkühlzeit seit dem letzten Einstieg verstrichen ist.
   - Das Hinzufügen eines weiteren Lots das Netto-Exposure innerhalb von `MaxPositions * OrderVolume` hält.
5. Wenn ein Signal erscheint, während eine entgegengesetzte Position gehalten wird, schließt die Strategie zuerst diese Position und wartet auf die nächste Kerze, bevor sie möglicherweise in die neue Richtung einsteigt.
6. Schützende Stop-Loss-, Take-Profit- und Trailing-Stop-Niveaus werden in Pips ausgedrückt und automatisch in Preis-Offsets basierend auf der Tick-Größe des Instruments umgerechnet.

## Risikomanagement

- **Stop Loss / Take Profit**: Optionale Niveaus in Pips. Sie werden bei jeder Positionsänderung neu berechnet und bei jeder abgeschlossenen Kerze geprüft.
- **Trailing Stop**: Aktiviert wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind. Sobald sich der Trade um mindestens `TrailingStop + TrailingStep` bewegt hat, folgt der Stop dem Preis mit dem konfigurierten Schritt.
- **Positionslimit**: Die absolute Nettoposition kann `MaxPositions * OrderVolume` nicht überschreiten.
- **Einstiegspause**: Die Strategie erfasst den Zeitstempel jeder Ausführung und erzwingt eine Pause bevor ein weiterer Trade eröffnet wird.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|----------|--------------|
| `OrderVolume` | 0.1 | Volumen jeder Marktorder. |
| `MaxPositions` | 10 | Maximale Anzahl erlaubter Lots pro Richtung. |
| `AnalysisPeriod` | 18 | Anzahl der abgeschlossenen Kerzen beim Zählen bullischer versus bärischer Bewegungen. |
| `PauseBetweenTradesSeconds` | 540 | Abkühlzeit in Sekunden nach einem Einstieg bevor ein neuer eröffnet werden kann. |
| `StartHour` | 3 | Startzeit (einschließlich) des Handelsfensters, Serverzeit. |
| `EndHour` | 6 | Endzeit (einschließlich) des Handelsfensters, Serverzeit. |
| `StopLossPips` | 50 | Stop-Loss-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TakeProfitPips` | 150 | Take-Profit-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TrailingStopPips` | 15 | Trailing-Stop-Abstand in Pips. Auf 0 setzen zum Deaktivieren. |
| `TrailingStepPips` | 5 | Zusätzlicher Abstand in Pips vor der Aktualisierung des Trailing Stops. Muss positiv sein wenn Trailing aktiviert ist. |
| `CandleType` | 15-Minuten-Zeitrahmen | Kerzenquelle für alle Berechnungen. |

## Implementierungshinweise

- Die High-Level-StockSharp-API wird durchgängig verwendet (`SubscribeCandles`, `.Bind`, `BuyMarket`, `SellMarket`).
- Einstiegszeitstempel werden innerhalb von `OnPositionChanged` erfasst, um die Pausenlogik genau wie der ursprüngliche EA zu implementieren, der nach jedem Einstieg eine feste Zeit wartete.
- Die Pip-Größe wird aus `Security.PriceStep` und `Security.Decimals` abgeleitet; für 3- oder 5-stellige Instrumente wird der Multiplikator automatisch angepasst.
- Ausstiege werden bei abgeschlossenen Kerzen durch Vergleich von Hoch/Tief mit den berechneten Stop- und Zielniveaus verwaltet.
- Trailing Stops imitieren die MetaTrader-Logik: Sie beginnen sich erst zu bewegen, wenn der Gewinn `TrailingStop + TrailingStep` übersteigt und kehren sich nie um.

## Verwendungstipps

1. `OrderVolume` und `MaxPositions` an die Kontraktgröße Ihres Brokers anpassen, um das Exposure unter Kontrolle zu halten.
2. `AnalysisPeriod` entsprechend dem Kerzen-Zeitrahmen wählen. Kürzere Zeitrahmen erfordern meist größere Fenster um Rauschen zu vermeiden.
3. Das Handelsfenster an die aktiven Stunden des Instruments anpassen (z. B. europäische Session für EUR-basierte Paare).
4. Verschiedene Kombinationen von Stop, Ziel und Trailing-Einstellungen backtesten – der ursprüngliche EA lief oft entweder mit festen Zielen oder Trailing Stops abhängig von den Marktbedingungen.

## Unterschiede zur MQL-Version

- Der StockSharp-Port verwendet ein Netto-Exposure-Modell. Beim Wechsel der Richtung wird die bestehende Position zuerst geschlossen, während die MetaTrader-Version abgesicherte Positionen halten konnte.
- Logging und Parameterverwaltung nutzen StockSharp-Funktionen, was Optimierung und UI-Integration erleichtert.
- Der Trailing Stop wird bei abgeschlossenen Kerzen ausgewertet, was mit anderen StockSharp-Strategie-Ports konsistent ist und das Reagieren auf unvollständige Balken vermeidet.

Mit diesen Überlegungen kann die JK Synchro-Strategie direkt innerhalb des StockSharp-Ökosystems gehandelt, analysiert und optimiert werden.
