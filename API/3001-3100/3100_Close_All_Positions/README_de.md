# 3100 Alle Positionen Schließen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
- Konvertiert das MQL5-Dienstprogramm **Close all positions** in eine StockSharp High-Level-Strategie.
- Beobachtet abgeschlossene Kerzen des konfigurierten Zeitrahmens und akkumuliert den schwebenden Gewinn jeder offenen Position im zugewiesenen Portfolio.
- Wenn der schwebende Gewinn dem Schwellenwert entspricht oder diesen überschreitet, werden Market-Orders gesendet, um alle von der Strategie verwalteten Wertpapiere zu schließen (einschließlich Kind-Strategien), bis das Buch vollständig geschlossen ist.
- Das `_closeAllRequested`-Flag spiegelt die MQL-Variable `m_close_all` wider, sodass Ausstiegsorders weiterhin ausgegeben werden, bis keine Positionen mehr vorhanden sind.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `ProfitThreshold` | `decimal` | `10` | Schwebender Gewinn (in Kontowährung), der erforderlich ist, bevor die Strategie alle offenen Positionen schließt. Spiegelt `InpProfit` aus dem EA wider. |
| `CandleType` | `DataType` | `1m`-Zeitrahmen | Kerzenserie, die die "neuer Balken"-Momente definiert. Die Gewinnprüfung wird nur beim Abschluss einer Kerze ausgeführt und emuliert die ursprüngliche `PrevBars`-Logik. |

## Handelslogik
1. Die Strategie abonniert Kerzen von `CandleType` und verarbeitet nur abgeschlossene Balken, genau wie der EA den Gewinn nur bei einem neuen Balken auswertete.
2. Bei jedem abgeschlossenen Balken ruft der Helper `CalculateTotalProfit` `Portfolio.CurrentProfit` ab (schwebender PnL einschließlich Provision und Swap). Wenn der Adapter diesen Wert nicht bereitstellen kann, fällt er auf die Summe individueller Positions-`PnL`-Werte zurück.
3. Wenn der berechnete schwebende Gewinn unter `ProfitThreshold` liegt, passiert nichts.
4. Sobald der Gewinn den Schwellenwert erreicht, wird `_closeAllRequested` auf `true` gesetzt und `CloseAllPositions()` wird sofort ausgeführt.
5. `CloseAllPositions()` sammelt jedes Wertpapier, das eine Exposure im Portfolio oder in verschachtelten Strategien hat, und sendet Market-Orders in die entgegengesetzte Richtung des aktuellen Volumens (Long → Verkauf, Short → Kauf).
6. Das `_closeAllRequested`-Flag bleibt gesetzt, bis `HasAnyOpenPosition()` erkennt, dass das Portfolio flach ist, entsprechend dem MQL-Verhalten, wo `m_close_all` wahr blieb, bis alle Tickets geschlossen waren.

## Weitere Hinweise
- Es wird nur die C#-Implementierung bereitgestellt; der Python-Ordner wird gemäß den Aufgabenanforderungen absichtlich leer gelassen.
- Die Strategie storniert keine ausstehenden Orders, da das ursprüngliche Skript nur Market-Positionen schloss.
- `SetOptimize` auf `ProfitThreshold` verwenden, um alternative Gewinnziele durch den Designer-Optimierer zu erkunden, falls erforderlich.

## Dateien
- `CS/CloseAllPositionsStrategy.cs`
