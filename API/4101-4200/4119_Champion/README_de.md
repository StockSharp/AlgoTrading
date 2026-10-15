# 4119 Champion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Bei dieser Strategie handelt es sich um einen High-Level-C#-Port des Expert Advisors MetaTrader, der sich in `MQL/919/champion.mq5` befindet. Der ursprüngliche EA wartet auf ein Signal des Relative Strength Index (RSI) und platziert drei Stop-Orders in Richtung des erwarteten Ausbruchs. Jede ausstehende Order beinhaltet bereits einen Stop-Loss und Take-Profit und der Stop-Loss wird immer dann nachgezogen, wenn sich der Preis günstig entwickelt. Die StockSharp-Version behält das gleiche Verhalten bei, verlässt sich jedoch ausschließlich auf API-Aufrufe auf hoher Ebene (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop` usw.).

Die Standardkonfiguration zielt auf liquide FX-Instrumente ab, bei denen der „Punkt“ MetaTrader mit dem „Punkt“ StockSharp `PriceStep` übereinstimmt (normalerweise 0,0001). Der Kerzentyp ist konfigurierbar und die Strategie kann auf jeden Zeitrahmen angewendet werden, solange das Konto die besten Geld-/Briefkurse und optional Informationen zum Stop-Level bietet.

## Strategielogik
1. **Signalerzeugung**
   - Für abgeschlossene Kerzen wird ein RSI mit konfigurierbarer Länge berechnet.
   - Der vorherige RSI-Wert (vor einem geschlossenen Balken) wird mit einem symmetrischen Schwellenwert (`RsiLevel`) verglichen.
   - `RSI < RsiLevel` löst ein bullisches Setup aus; `RSI > 100 - RsiLevel` löst ein rückläufiges Setup aus.
2. **Ausstehende Auftragserteilung**
   - Wenn keine offenen Positionen und keine aktiven Pending Orders vorhanden sind, die von der Strategie verwaltet werden, werden drei identische Stop Orders in der signalisierten Richtung platziert.
   - Kaufstopps werden über dem besten Briefkurs platziert, Verkaufsstopps unter dem besten Geldkurs. Die Entfernung berücksichtigt das vom Server bereitgestellte Stoppniveau (falls verfügbar) oder den `MinOrderDistancePoints`-Fallback.
   - Das Bestellvolumen wird dynamisch berechnet: verfügbarer Kontowert dividiert durch `BalancePerLot`, auf den Losbereich `[0.1, 15]` begrenzt und auf zwei Dezimalstellen gerundet. Jeder ausstehende Auftrag erhält ein Drittel des berechneten Volumens.
3. **Erste Schutzanordnungen**
   - Sobald der erste Trade ausgeführt wird, werden aggregierte Schutzaufträge registriert: Stop-Loss bei `entry ± StopLossPoints` und Take-Profit bei `entry ± TakeProfitPoints` (MetaTrader Punkte umgerechnet in Preis von `PriceStep`).
   - Wenn `TakeProfitPoints` Null ist, ist die Take-Profit-Order deaktiviert.
4. **Trailing Stop**
   - Während eine Position offen ist, wird die Stop-Loss-Order bei jeder Aktualisierung der Stufe 1 verschärft.
   - Für Long-Positionen beträgt der neue Stop `max(entry + spread, bid - StopLoss)`; für Kurzfilme `min(entry - spread, ask + StopLoss)`.
   - Das Trailing wird nur aktiviert, wenn die Bewegung die Summe aus dem Broker-Stop-Level und dem aktuellen Spread überschreitet, wodurch die ursprünglichen EA-Schutzmaßnahmen reproduziert werden.
5. **Ausstehende Auftragswartung**
   - Ausstehende Kaufstopps werden näher an den Markt verschoben, wenn ihr Aktivierungspreis mehr als `RepriceDistancePoints` vom aktuellen Briefkurs entfernt ist. Die gleiche Logik gilt für Verkaufsstopps im Vergleich zum aktuellen Gebot.
   - Bei der Neubewertung wird immer der größere Wert von `RepriceDistancePoints` und der effektiven Stopp-Level-Distanz berücksichtigt.
6. **Positionsausgang**
   - Positionen werden über die schützenden Stop-Loss-/Take-Profit-Orders oder durch manuelles Eingreifen des Benutzers geschlossen. Wenn die Positionsgröße auf Null zurückkehrt, storniert die Strategie alle verbleibenden Schutzaufträge und wartet auf das nächste RSI-Signal.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TakeProfitPoints` | MetaTrader Punkte werden vom Ausführungspreis addiert/subtrahiert, um die Take-Profit-Order zu platzieren. Auf `0` setzen, um das Ziel zu deaktivieren. |
| `StopLossPoints` | MetaTrader Punkte werden vom Ausführungspreis addiert/subtrahiert, um die Stop-Loss-Order zu platzieren und die Trailing-Distanz zu berechnen. |
| `RsiPeriod` | RSI Länge (Anzahl der Kerzen). |
| `RsiLevel` | Symmetrischer Schwellenwert RSI. Werte unterhalb des Niveaus lösen Long-Positionen aus, Werte über `100 - level` lösen Short-Positionen aus. |
| `BalancePerLot` | Der Betrag in der Kontowährung wird bei der Größenbestimmung der Positionen als einem Standardlos entsprechend betrachtet. |
| `MinOrderDistancePoints` | Fallback-Mindestabstand (in Punkten) zwischen dem Marktpreis und neuen Stop-Orders, wenn der Handelsplatz kein Stop-Level meldet. |
| `RepriceDistancePoints` | Entfernung (in Punkten), die eine Neubewertung der ausstehenden Bestellung auslöst. |
| `CandleType` | Kerzendatentyp, der für die RSI-Berechnung verwendet wird. |

## Nutzungshinweise
- Die Strategie erfordert sowohl Kerzendaten als auch Level-1-Kurse (bester Geld-/Briefkurs). Ohne Level-1-Updates sind die Trailing-Logik und die Wartung ausstehender Bestellungen deaktiviert.
- Wenn der Broker einen Stopp-Level oder eine Stopp-Distanz über Level-1-Metadaten offenlegt, wird dieser automatisch berücksichtigt. Andernfalls konfigurieren Sie `MinOrderDistancePoints` entsprechend den Geräteanforderungen.
- Die Positionsgröße wird immer dann auf die Eigenschaft `Strategy.Volume` zurückgegriffen, wenn Portfolioinformationen fehlen oder die berechnete Losgröße nicht mehr positiv ist.
- Drei ausstehende Orders werden immer zusammen aufgegeben. Stornieren Sie unerwünschte Bestellungen manuell, wenn eine teilweise Teilnahme erforderlich ist. Die Strategie wird weiterhin die verbleibenden verwalten.

## Risikomanagement
- Stop-Loss- und Take-Profit-Orders sind native Börsen-/Broker-Orders, die das Verhalten des MetaTrader EA widerspiegeln. Wenn eine Position geschlossen wird, werden die Schutzaufträge sofort gelöscht.
- Der Trailing Stop bewegt sich nur in Richtung Gewinn und lockert niemals den Stop-Loss. Es wird aktiviert, sobald der Preis mindestens `(StopLevel + spread)` über dem Einstiegspreis liegt.
- Die Neubewertungslogik verhindert, dass veraltete ausstehende Aufträge nach großen Sprüngen zurückbleiben, wodurch die Wahrscheinlichkeit verzögerter Ausführungen verringert wird.
