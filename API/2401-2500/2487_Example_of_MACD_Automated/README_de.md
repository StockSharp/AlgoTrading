# Beispiel einer automatisierten MACD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die Strategie repliziert den MetaTrader 4 Expert Advisor „Example of MACD Automated" mithilfe der StockSharp High-Level-API. Sie überwacht die MACD-Hauptlinie auf zwei Zeitrahmen und öffnet eine einzelne Position, wenn beide Trendfilter übereinstimmen. Schützende Stop-Loss- und Take-Profit-Abstände werden in Preisschritten angegeben, und die Positionsgröße folgt der originalen AdvancedMM-Logik, die das Volumen der jüngsten Verlusttrades akkumuliert.

## Handelslogik

1. **Filter auf höherem Zeitrahmen** – ein MACD (12, 26, 9), berechnet auf dem höheren Zeitrahmen (Standard: Tageskerzen), muss eine positive Hauptlinie für Long-Signale oder eine negative Hauptlinie für Short-Signale haben.
2. **Bestätigung des Einstiegszeitrahmen** – dieselben MACD-Einstellungen auf dem Einstiegszeitrahmen (Standard: 15-Minuten-Kerzen) müssen in dieselbe Richtung wie der Filter des höheren Zeitrahmens zeigen.
3. **Einzelposition** – die Strategie handelt eine Position gleichzeitig. Neue Einstiege werden übersprungen, bis die bestehende Position durch Schutzlevel geschlossen wird.
4. **Schutzorders** – Stop-Loss- und Take-Profit-Level werden in Vielfachen des Instrumentpreisschritts gemessen, entsprechend den ursprünglichen MT4-Eingaben `StopLoss` und `TakeProfit`. Ein Wert von `0` deaktiviert den entsprechenden Schutz.
5. **Erweitertes Money Management** – das Handelsvolumen steigt nach aufeinanderfolgenden Verlusttrades durch Summierung der Lotgrößen der Verluste und kehrt nach profitablen Trades zum Basisvolumen zurück, was die `AdvancedMM()`-Funktion des Quell-EAs emuliert.

## Parameter

| Name | Beschreibung | Standard |
| ---- | ------------ | -------- |
| `BaseVolume` | Basis-Ordervolumen, das von der AdvancedMM-Logik verwendet wird. | `0.01` |
| `StopLossPoints` | Stop-Loss-Abstand in Preisschritten. `0` deaktiviert den Stop. | `50` |
| `TakeProfitPoints` | Take-Profit-Abstand in Preisschritten. `0` deaktiviert das Ziel. | `30` |
| `MacdFastLength` | Schnelle EMA-Periode des MACD auf beiden Zeitrahmen. | `12` |
| `MacdSlowLength` | Langsame EMA-Periode des MACD. | `26` |
| `MacdSignalLength` | Signallinie EMA-Periode. | `9` |
| `EntryCandleType` | Zeitrahmen für die Trade-Ausführung. | `15m`-Kerzen |
| `FilterCandleType` | Höherer Zeitrahmen als Trendfilter. | `1d`-Kerzen |

## Positionsmanagement

- Stop-Loss- und Take-Profit-Preise werden bei jeder neuen Position basierend auf dem Instrumentpreisschritt neu berechnet.
- Wenn ein Schutzniveau innerhalb einer Bar berührt wird, nimmt die Strategie an, dass die Order zu diesem Level ausgeführt wird, und erfasst den realisierten Gewinn oder Verlust.
- Nach jedem geschlossenen Trade aktualisiert die AdvancedMM-Logik die nächste Ordergröße:
  - Weniger als zwei historische Trades → Basisvolumen verwenden.
  - Der jüngste Trade war ein Verlust → sein Volumen wiederholen.
  - Aufeinanderfolgende Verluste vor dem letzten Gewinn → ihre Volumina summieren, um zu erholen.
  - Andernfalls → zum Basisvolumen zurückkehren.

## Hinweise

- Die Konvertierung behält das ursprüngliche Verhalten bei, eine Position bis zum Erreichen eines Schutzlevels zu halten; es gibt keine Ausgänge bei MACD-Kreuzungen.
- Stellen Sie sicher, dass das Instrument gültige `PriceStep`-Informationen hat, damit punktbasierte Stop- und Zielabstände korrekt berechnet werden.
- Die Strategie basiert auf abgeschlossenen Kerzen und sollte mit historischen Daten oder Live-Feeds verwendet werden, die fertige Kerzenaktualisierungen liefern.
