# N Candles Sequenz-Einstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Konzept
Die N-Candles-Strategie scannt den Markt nach aufeinanderfolgenden Kerzen, die alle in dieselbe Richtung schließen. Sobald eine konfigurierbare Anzahl bullischer oder bärischer Kerzen erschienen ist, geht die Strategie in Richtung der Sequenz ein. Die Implementierung ist eine direkte Konvertierung des MetaTrader "N Candles v4" Expert Advisors und bewahrt seine Risikokontrollen, Pip-basierte Konfiguration und optionales Trailing-Stop-Verhalten innerhalb der High-Level-API von StockSharp.

## Einstiegsbedingungen
- Jede abgeschlossene Kerze wird einmal ausgewertet.
- Kerzen, die nach oben schließen, werden als bullisch gezählt, Kerzen, die nach unten schließen, als bärisch, und Doji-Kerzen setzen die Sequenz zurück.
- Wenn `ConsecutiveCandles` bullische (oder bärische) Kerzen in einer Reihe erscheinen, sendet die Strategie eine Market-Order in Richtung der Bewegung.
- Hedging-ähnliches Stapeln oder Netting-ähnliche Exposure-Obergrenzen werden je nach gewähltem `AccountingMode` angewendet.

## Exit-Management
- `StopLossPips` und `TakeProfitPips` definieren statische Exit-Niveaus, gemessen in Pips vom durchschnittlichen Einstiegspreis der aktiven Position.
- Wenn `TrailingStopPips` größer als null ist, folgt das Stop-Niveau dem günstigsten Preis:
  - Wenn kein fester Stop vorhanden ist (beispielsweise wenn `StopLossPips` null ist), wartet die Strategie, bis der Preis sich um `TrailingStopPips` zugunsten des Trades bewegt, bevor ein Break-Even-Stop gesetzt wird.
  - Sobald ein Stop gesetzt ist, bewegt er sich in Richtung Markt, wenn der Abstand zwischen Preis und Stop `TrailingStopPips + TrailingStepPips` überschreitet.
- Schutz-Niveaus werden neu berechnet, wenn sich die Positionsgröße ändert, und gegen jede abgeschlossene Kerze geprüft, was garantiert, dass jedes Stop-Loss- oder Take-Profit-Ereignis den Trade sofort schließt.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `ConsecutiveCandles` | Anzahl identischer Kerzen, die zum Auslösen eines Einstiegs erforderlich sind. | 3 |
| `TakeProfitPips` | Take-Profit-Abstand in Pips. Null zur Deaktivierung des Ziels verwenden. | 50 |
| `StopLossPips` | Stop-Loss-Abstand in Pips. Null zur Deaktivierung des Stops verwenden. | 50 |
| `TrailingStopPips` | Trailing-Stop-Abstand in Pips. Null deaktiviert Trailing. | 10 |
| `TrailingStepPips` | Zusätzliche Bewegung, die erforderlich ist, bevor der Trailing-Stop vorrückt. | 4 |
| `MaxPositionsPerDirection` | Maximale Anzahl gestapelter Einstiege pro Richtung beim Hedging. | 2 |
| `MaxNetVolume` | Maximale absolute Netto-Positionsgröße bei Netting-Modus. | 2 |
| `AccountingMode` | Wechsel zwischen `Netting` (Volumen-Obergrenze) und `Hedging` (Einstiegsanzahl-Obergrenze). | Netting |
| `CandleType` | Kerzen-Aggregation für die Mustererkennung. | 1-Minuten-Kerzen |

Alle Pip-basierten Parameter werden anhand der Tick-Größe des Instruments in Preisoffsets umgerechnet. Wenn das Instrument 3 oder 5 Dezimalstellen hat, wird die Pip-Größe um den Faktor zehn skaliert, um die MetaTrader-Definition zu spiegeln.

## Implementierungshinweise
- Die Strategie basiert auf dem StockSharp High-Level-Kerzenabonnement (`SubscribeCandles`) und vermeidet manuelle Verlaufspuffer.
- Die Schutzlogik verfolgt den höchsten (für Longs) oder niedrigsten (für Shorts) Preis seit dem Einstieg, um das ursprüngliche Trailing-Verhalten zu emulieren.
- Positionslimits passen sich automatisch an das Basis-`Volume` der Strategie an. Eine Erhöhung des `Volume` erweitert Stop- und Take-Profit-Ordergrößen proportional.
- Protokollmeldungen werden ausgegeben, wenn ein Schutz-Exit (Stop oder Take-Profit) eine Position schließt, was Klarheit während Backtests bietet.

## Verwendungstipps
- `Hedging`-Modus wählen, wenn Plattformen simuliert werden, die mehrere Tickets pro Richtung erlauben, oder bei `Netting` bleiben, um Einzel-Positions-Konten zu spiegeln.
- `TrailingStepPips` auf null setzen für einen klassischen Trailing-Stop, der sich immer bewegt, wenn der Markt um `TrailingStopPips` vorrückt.
- Da Exits auf abgeschlossenen Kerzen ausgewertet werden, ein kürzeres Kerzenintervall in Betracht ziehen, wenn Intrabar-Präzision entscheidend ist.
