# Ten Pips EURUSD-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Ten Pips EURUSD-Strategie** ist ein Ausbruchssystem, das die Logik des ursprünglichen MetaTrader-Expert-Advisors reproduziert. Sie beobachtet die zuletzt abgeschlossene Kerze und platziert Stop-Orders oberhalb und unterhalb dieses Bereichs. Orders werden in Pips dimensioniert, an die aktuelle Instrument-Tick-Größe angepasst, und optional durch einen Trailing Stop verwaltet. Die Implementierung verwendet StockSharp's High-Level-Kerzen-Subscriptions zusammen mit Orderbuch-Updates, um das Verhalten nahe an der MQL-Version zu halten, während sie broker-neutral bleibt.

## Strategie-Logik
1. Den ausgewählten Kerzentyp abonnieren und warten, bis ein neuer Balken aktiv wird.
2. Das vorherige Kerzenhoch und -tief erfassen, wenn dieser Balken endet. Ausstehende Orders werden zu diesem Zeitpunkt storniert, weil der ursprüngliche EA sie auf eine Kerze beschränkt.
3. Beim ersten Tick der neuen Kerze prüfen, dass:
   - Die aktuelle Eröffnung innerhalb des vorherigen Kerzenbereichs liegt (Gap-Filterung).
   - Der aktuelle Preis mindestens drei Pip-Einheiten von beiden Extremen entfernt ist (ein Proxy für den Broker-Stop-Level).
4. Den aktuellen Spread mithilfe des besten Bid/Ask berechnen. Wenn keine Level-1-Daten vorhanden sind, fällt die Strategie auf die Pip-Größe zurück.
5. Zwei ausstehende Stop-Orders platzieren:
   - **Buy Stop**: Aktivierung bei `vorherigem Hoch + 2 × Spread` mit Stop-Loss unter dem Einstiegspreis um `StopLossPips` und, wenn Trailing deaktiviert ist, Take-Profit bei `vorherigem Hoch + 2 × Spread + TakeProfitPips`.
   - **Sell Stop**: Aktivierung bei `vorherigem Tief − Spread` mit symmetrischen Exit-Levels.
6. Sobald die Kerze abgeschlossen ist oder beide Orders gefüllt/storniert wurden, wiederholt sich der Prozess für die nächste Kerze.

### Positionsmanagement
- Während eine Position offen ist, überwacht die Strategie den besten Bid/Ask bei jedem Orderbuch-Update.
- Wenn Trailing deaktiviert ist, schließt die Position, wenn der Preis den festen Stop oder Take-Profit berührt.
- Wenn Trailing aktiviert ist:
  - Bei Long-Trades aktiviert sich der Trailing Stop, sobald der Preis um `TrailingStopPips` vorrückt. Der Stop wird auf `Bid − TrailingStopPips` gesetzt und bewegt sich jedes Mal, wenn der Preis sich um mindestens `TrailingStepPips` verbessert.
  - Bei Short-Trades spiegelt die Logik die Long-Seite mit dem Ask-Preis.
- Manuelle Exits setzen alle Schutzniveaus zurück und lassen jede ausstehende entgegengesetzte Stop-Order bis zum Ende der Kerze aktiv, was das Straddle-Verhalten des EA reproduziert.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `0.01` | Ordervolumen in Lots (oder Kontrakteinheiten für Nicht-FX-Symbole). |
| `StopLossPips` | `50` | Abstand zwischen Einstieg und Schutz-Stop, ausgedrückt in Pips. |
| `TakeProfitPips` | `150` | Take-Profit-Abstand in Pips, wird nur verwendet, wenn Trailing deaktiviert ist. |
| `UseTrailing` | `false` | Aktiviert die Trailing-Stop-Logik. |
| `TrailingStopPips` | `50` | Initialer Abstand für den Trailing Stop, gemessen in Pips. |
| `TrailingStepPips` | `25` | Mindestgewinn (in Pips) zum Bewegen eines aktiven Trailing Stops. |
| `CandleType` | `15-Minuten-Zeitrahmen` | Kerzenserie zur Erkennung der Ausbruchsniveaus. |

## Hinweise und Empfehlungen
- Die Pip-Größe wird automatisch aus `Security.PriceStep` abgeleitet und emuliert die MQL-Ziffernanpassung, sodass sich die Strategie an FX-Symbole mit 3 und 5 Stellen anpasst.
- Alle Abstände werden vor dem Platzieren von Orders in Preiseinheiten neu berechnet, was die Strategie mit Nicht-FX-Assets kompatibel hält, solange die Tick-Größe definiert ist.
- Der minimale Stop-Level-Fallback (drei Pip-Einheiten) imitiert das Verhalten des ursprünglichen EAs, wenn der Broker keinen Stop-Level meldet.
- Da ausstehende Orders am Ende jeder Kerze ablaufen, sollten Sie die Strategie auf dem gewünschten Zeitrahmen ohne Lücken im eingehenden Kerzenstrom ausführen.
- Risikomanagement ist entscheidend. Erwägen Sie das Testen mit realistischen Spreads und Provisionsmodellen, bevor Sie mit echtem Kapital handeln.
