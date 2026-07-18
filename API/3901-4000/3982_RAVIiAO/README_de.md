# RAVIiAO-Strategie (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **RAVIiAO-Strategie** reproduziert den MetaTrader 4 Expertenberater „RAVIiAO“ innerhalb der StockSharp hohen Ebene API. Das System
wartet darauf, dass sich eine neue Kerze schließt, wertet die Steigung des RAVI-Oszillators zusammen mit Bill Williams' Beschleunigung/Verzögerung (AC) aus.
Oszillator und eröffnet eine Position sofort zum Markt, wenn beide Indikatoren über die Trendrichtung übereinstimmen. Der Port behält die
Ursprünglicher Parametersatz – gleitende Durchschnittsperioden, Schwellenwert, Stop-Loss/Take-Profit-Abstände und Auftragsvolumen – ermöglicht Händlern
um das alte Verhalten ohne manuelle Anpassungen zu reproduzieren.

## Kern-Workflow
1. **Kerzenabonnement** – Die Strategie abonniert einen konfigurierbaren Zeitrahmen (standardmäßig 30-Minuten-Kerzen).
2. **Indikatoraktualisierungen** – bei jeder fertigen Kerze werden zwei einfache gleitende Durchschnitte aktualisiert, um den RAVI-Oszillator und die Feeds zu erstellen
die gleiche Kerze in ein Awesome Oscillator + 5-Perioden-Glättungspaar, um den AC-Wert zu erhalten.
3. **Signalvorbereitung** – die letzte fertige Kerze wird als „Balken 1“ gespeichert, während der vorherige Wert entsprechend „Balken 2“ wird
`iCustom(...,1)` und `iCustom(...,2)` Anrufe von MetaTrader.
4. **Eintrittsentscheidung** – eine Long-Position wird eröffnet, wenn sowohl AC als auch RAVI über ihre vorherigen Werte steigen und a bestätigen
bullisches Umfeld (`AC[1] > AC[2] > 0` und `RAVI[1] > RAVI[2] > Threshold`). Bei Short-Trades gelten die gespiegelten Konditionen.
5. **Risikomanagement** – sobald ein Auftrag ausgeführt wird, zeichnet die Strategie statische Stop-Loss- und Take-Profit-Werte auf, ausgedrückt in
Instrumentenpunkte (d. h. `StopLossPoints * PriceStep`). Kerzen werden anhand ihrer Höchst-/Tiefstkurse auf Intrabar-Verstöße überwacht.
6. **Status-Reset** – wenn ein Schutzniveau erreicht wird, wird die Position mit einer Marktorder geschlossen und die internen Puffer werden zurückgesetzt
für die nächste Gelegenheit.

## Handelsregeln
- **Lange Einträge**
  - Der vorherige AC-Wert liegt über dem früheren AC-Wert und beide sind größer als Null.
  - Der vorherige RAVI-Wert liegt sowohl über dem Schwellenwert als auch über dem früheren RAVI-Wert.
  - Zum Zeitpunkt des Signals keine aktive Position.
- **Kurze Einträge**
  - Der vorherige AC-Wert liegt unter dem früheren AC-Wert und beide unter Null.
  - Vorheriger RAVI-Wert liegt unter dem negativen Schwellenwert und unter dem früheren RAVI-Wert.
  - Keine aktive Position, wenn das Signal ausgelöst wird.
- **Positionsausgänge**
  - Statische Stop-Loss- und Take-Profit-Level werden in Rohpunkten ausgedrückt und über das Instrument `PriceStep` in Preis-Offsets umgewandelt.
  - Verstöße werden bei Candle-Extremen (Tief für Long-Stops, Hoch für Short-Stops usw.) erkannt und sofort über den Markt geschlossen
Befehle, um die Schutzbefehle von MetaTrader nachzuahmen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Für das Kerzenabonnement verwendeter Zeitrahmen (Standard 30 Minuten). |
| `FastLength` | Schnell gleitende Durchschnittslänge, die im RAVI-Oszillator verwendet wird. |
| `SlowLength` | Langsam gleitende Durchschnittslänge, die im RAVI-Oszillator verwendet wird. |
| `Threshold` | Minimaler absoluter RAVI-Prozentsatz zur Validierung einer Trendfortsetzung. |
| `StopLossPoints` | Stop-Loss-Distanz in Instrumentenpunkten (multipliziert mit `PriceStep`). |
| `TakeProfitPoints` | Take-Profit-Distanz in Instrumentenpunkten. |
| `TradeVolume` | Market-Order-Volumen für jeden Eintrag. |

## Konvertierungshinweise
- Der StockSharp-Port speichert die beiden aktuellsten Indikatorwerte, sodass die Entscheidung bei Kerze *n* die `AC[1]` und wiederverwendet
`RAVI[1]`-Werte aus MetaTrader (d. h. Ergebnisse des vorherigen Balkens), wobei der Ausführungsstil „Neuer Balken“ des EA erhalten bleibt.
- AC wird durch die Differenz zwischen dem Awesome Oscillator und seinem einfachen gleitenden 5-Perioden-Durchschnitt neu aufgebaut, passend zum MT4
Berechnungskette.
- Stopps und Ziele werden anhand der Kerzenextreme bewertet, anstatt ausstehende Schutzaufträge zu erteilen. Dies spiegelt den Effekt wider
der integrierten SL/TP-Verarbeitung von MetaTrader, während die Implementierung idiomatisch für StockSharp bleibt.

## Nutzungstipps
- Stellen Sie sicher, dass das ausgewählte Instrument ein korrektes `PriceStep` bereitstellt; andernfalls entsprechen die Schutzabstände nicht der MT4-Version.
- Optimieren Sie die Parameter `Threshold`, `FastLength` und `SlowLength`, wenn Sie die Strategie an Märkte mit unterschiedlichen Anforderungen anpassen
Volatilitätsmerkmale.
- Kombinieren Sie die Strategie mit StockSharp Schutzmaßnahmen auf Portfolio- oder Connector-Ebene für zusätzliche Sicherheit beim Live-Handel.
