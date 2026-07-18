# RSI Expert Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Port der MetaTrader 5 "RSI_Expert"-Strategie, die RSI-Schwellenwert-Ausbrüche handelt.
- Verwendet einen einzigen RSI-Indikator, um Momentum-Umkehrungen in der Nähe von überverkauften/überkauften Bereichen zu erkennen.
- Implementiert das ursprüngliche feste Take-Profit-, Stop-Loss- und Trailing-Stop-Management in Pips.

## Strategie-Logik
1. RSI auf der ausgewählten Kerzenserie aufbauen (Standardperiode: 14).
2. Die zwei zuletzt abgeschlossenen RSI-Werte verfolgen.
3. Long gehen, wenn der RSI nach vorherigem Unterschreiten wieder über den unteren Schwellenwert steigt (Standard 20).
4. Short gehen, wenn der RSI nach vorherigem Überschreiten wieder unter den oberen Schwellenwert fällt (Standard 60).
5. Jede entgegengesetzte Exposition schließen, bevor eine neue Position eröffnet wird, um netto-direktional zu bleiben.
6. Offene Trades mit optionalen Stop-Loss-, Take-Profit- und Trailing-Stop-Abständen in Pips verwalten.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `CandleType` | Zeitrahmen für die Kerzenaggregation. | 1-Stunden-Kerzen |
| `TradeVolume` | Ordergröße für Einstiege. | 0.1 |
| `RsiPeriod` | RSI-Lookback-Länge. | 14 |
| `RsiUpperLevel` | RSI-Schwellenwert, der eine bärische Umkehr signalisiert. | 60 |
| `RsiLowerLevel` | RSI-Schwellenwert, der eine bullische Umkehr signalisiert. | 20 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips (0 deaktiviert). | 60 |
| `StopLossPips` | Stop-Loss-Abstand in Pips (0 deaktiviert). | 0 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips (0 deaktiviert Trailing). | 15 |
| `TrailingStepPips` | Mindestpreisverbesserung, bevor der Trailing-Stop erneut verschoben wird. | 5 |

> **Pip-Interpretation:** Im StockSharp-Port entspricht ein "Pip" einem `Security.PriceStep`. Bei FX-Symbolen mit fraktionierter Quotierung sicherstellen, dass der Preisschritt mit der Pip-Konvention des Instruments übereinstimmt, andernfalls die Eingangsabstände entsprechend anpassen.

## Risikomanagement
- Take-Profit und Stop-Loss werden bei jeder abgeschlossenen Kerze anhand des letzten durchschnittlichen Positionspreises ausgewertet.
- Der Trailing-Stop aktiviert sich erst, nachdem die Bewegung `TrailingStopPips + TrailingStepPips` überschreitet, und folgt dann dem Close um `TrailingStopPips`, wenn der Preis vorrückt.
- Stop-Überprüfungen verwenden Kerzen-Hochs/Tiefs, um Intrabar-Trigger zu emulieren; bei Auslösung wird die Position zum Marktpreis geschlossen.

## Konvertierungshinweise
- Die High-Level-API wird verwendet (`SubscribeCandles` + `Bind`), und RSI-Werte werden direkt aus dem Binding-Callback verarbeitet ohne manuelle Indikatorbuffer.
- Die Trailing-Stop-Logik reproduziert die MQL-Bedingungen, einschließlich des Schrittschwellenwerts vor jeder Anpassung.
- Die Strategie setzt den Trailing-Status zurück, wenn die Exposition wechselt oder schließt, um veraltete Niveaus in einen neuen Trade zu verhindern.
