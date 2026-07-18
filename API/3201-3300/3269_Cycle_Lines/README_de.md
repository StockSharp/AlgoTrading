# Cycle-Lines-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Cycle-Lines-Strategie ist die StockSharp-Portierung des MetaTrader Expert Advisors "Cycle Lines". Das ursprüngliche Skript kombinierte Chart-Zeichnungen mit manuellen Trade-Schaltflächen. Diese Portierung konzentriert sich auf die automatisierte Handelslogik, die diese Bedienelemente begleitete. Die Strategie handelt Kreuzungen der MACD-Linie, hält das Risiko über absolute Stop-Loss- und Take-Profit-Grenzen eng kontrolliert und unterstützt Break-even- sowie Trailing-Stop-Management.

Wenn die MACD-Linie ihre Signallinie nach oben kreuzt, eröffnet die Strategie eine Long-Position. Wenn die Linie die Signallinie nach unten kreuzt, eröffnet sie eine Short-Position. Offene Trades werden geschlossen, wenn der Indikator in die Gegenrichtung wechselt oder eine der Schutzregeln (Stop-Loss, Take-Profit, Break-even oder Trailing Stop) ausgelöst wird.

## Handelsregeln

1. **Einstiegsbedingungen**
   - **Long:** MACD kreuzt auf der ausgewählten Kerzenserie über die Signallinie.
   - **Short:** MACD kreuzt auf der ausgewählten Kerzenserie unter die Signallinie.
   - Einstiege werden erst bewertet, nachdem der Indikator vollständig ausgebildet ist und die Strategie verbunden sowie zum Handel zugelassen ist.
2. **Ausstiegsbedingungen**
   - Entgegengesetzte MACD-Kreuzung.
   - Stop-Loss erreicht.
   - Take-Profit erreicht.
   - Break-even-Schutzlevel berührt.
   - Trailing-Stop-Level berührt.

## Parameter

| Name | Beschreibung | Standard | Hinweise |
| ---- | ------------ | -------- | -------- |
| `Volume` | Ordervolumen pro Trade. | `1` | Muss positiv sein. |
| `MacdFastPeriod` | Periode der schnellen EMA innerhalb der MACD-Berechnung. | `12` | Optimierbar. |
| `MacdSlowPeriod` | Periode der langsamen EMA innerhalb von MACD. | `26` | Optimierbar. |
| `MacdSignalPeriod` | Periode der MACD-Signallinie. | `9` | Optimierbar. |
| `StopLoss` | Absolute Preisdistanz für den Schutz-Stop. | `0` | Deaktiviert, wenn auf `0` gesetzt. |
| `TakeProfit` | Absolute Preisdistanz für das Take-Profit-Ziel. | `0` | Deaktiviert, wenn auf `0` gesetzt. |
| `TrailingOffset` | Abstand zwischen dem besten günstigen Preis und dem Trailing Stop. | `0` | Deaktiviert, wenn auf `0` gesetzt. |
| `BreakEvenTrigger` | Gewinndistanz, die vor dem Verschieben des Stops auf Break-even erforderlich ist. | `0` | Deaktiviert, wenn auf `0` gesetzt. |
| `BreakEvenOffset` | Zusätzlicher Offset, der auf das Break-even-Niveau angewendet wird. | `0` | Ermöglicht, etwas zusätzlichen Gewinn ober-/unterhalb des Einstiegs zu sichern. |
| `CandleType` | Für Indikatorberechnungen verwendete Kerzenserie. | Kerzen im Zeitrahmen von `15` Minuten | Akzeptiert jeden von StockSharp unterstützten `DataType`. |

## Positionsverwaltung

- **Stop-Loss / Take-Profit:** Beide Prüfungen bewerten Intrabar-Extreme (Hoch/Tief) abgeschlossener Kerzen und stellen sicher, dass der Ausstieg die konfigurierte absolute Distanz zum Einstiegspreis einhält.
- **Break-even:** Sobald sich der Preis um mindestens `BreakEvenTrigger` zugunsten des Trades bewegt, aktiviert die Strategie einen Stop bei `entry ± BreakEvenOffset`. Jede Gegenbewegung, die dieses Niveau berührt, schließt die Position.
- **Trailing Stop:** Bei Long-Trades wird der höchste erreichte Preis überwacht. Das Stop-Niveau folgt dem Hoch abzüglich `TrailingOffset`. Bei Short-Trades spiegelt die Logik dieses Verhalten um den niedrigsten Preis.

## Nutzungshinweise

- Die Strategie handelt jeweils nur eine einzelne Position. Aufeinanderfolgende Signale pyramidisieren eine bestehende Position nicht.
- Parameter werden als `StrategyParam<T>`-Objekte bereitgestellt und unterstützen daher die Optimierung innerhalb von StockSharp.
- Um das Standardverhalten des ursprünglichen EA nachzubilden, setzen Sie die MACD-Perioden auf `12 / 26 / 9` und konfigurieren die Risikoparameter entsprechend den gewünschten Pip-Werten.

## Unterschiede zur MQL-Version

- Chart-Zeichnungsfunktionen und manuelle BUY/SELL-Schaltflächen wurden weggelassen, weil StockSharp Visualisierung anders handhabt.
- Alle Schutzregeln wurden so umgeschrieben, dass sie auf Kerzendaten statt auf MetaTrader-Tickereignissen arbeiten; dadurch bleiben sie mit der High-Level-API von StockSharp kompatibel.
- Trailing- und Break-even-Logik sind für Long- und Short-Trades symmetrisch, um Klarheit und Robustheit zu erhöhen.
