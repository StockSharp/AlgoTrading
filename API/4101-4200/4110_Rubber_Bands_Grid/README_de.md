# Gummiband-Gitterstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
- Konvertierung des MetaTrader 4 Expert Advisors **RUBBERBANDS_2.mq4**.
- Führt ein symmetrisches Raster um den aktuellen Preis aus, wobei die besten Geld-/Briefkurse anstelle von Kerzen verwendet werden.
- Führt separate Hauptbücher für Long- und Short-Engagements, damit das Verhalten mit der abgesicherten MT4-Implementierung übereinstimmt.
- Implementiert Gewinn- und Verlustkontrollen auf Sitzungsebene und einen manuellen Ruhe-/Stoppmodus, der mit den ursprünglichen Eingaben identisch ist.

## Handelslogik
1. Die Strategie abonniert `SubscribeLevel1()` und reagiert auf jede Änderung des besten Gebots und besten Briefs.
2. Zwei gleitende Extreme (`_upperExtreme` / `_lowerExtreme`) erfassen den höchsten und niedrigsten Briefkurs, der seit dem letzten Zurücksetzen erreicht wurde. Sie werden aus Parametern initialisiert, wenn `UseInitialValues` wahr ist, andernfalls wird der erste empfangene Briefkurs verwendet.
3. Wenn es keine offenen Trades gibt und die Serverzeit den ersten Tick einer Minute erreicht (Sekunde gleich Null), fordert die Strategie sowohl einen Marktkauf als auch einen Marktverkauf an. Dies spiegelt das MT4-Verhalten wider, bei dem jede Minute Kauf-/Verkaufsflags gesetzt werden, während das Buch leer ist.
4. Jedes Mal, wenn der Briefkurs um `GridStepPoints` Punkte über den gespeicherten Höchstwert steigt, wird ein neuer Verkaufsauftrag erteilt. Jeder Rückgang um die gleiche Distanz unter das gespeicherte Tief löst eine neue Kauforder aus. Die Extremwerte werden nach jedem Auslöser auf den aktuellen Brief aktualisiert, sodass sich die Leiter mit dem Preis „dehnt“.
5. Die Gesamtzahl der gleichzeitig offenen Trades (Summe aus Long- und Short-Legs) ist durch `MaxTrades` begrenzt.
6. Der variable Gewinn wird aus dem aktuellen Geld-/Briefkurs berechnet: Long-Gewinne basieren auf dem Geldkurs minus dem durchschnittlichen Long-Preis, Short-Gewinne basieren auf dem durchschnittlichen Short-Preis minus dem Briefkurs. Der Helfer `PriceToMoney` konvertiert Preisunterschiede mithilfe von `PriceStep`/`StepPrice` in die Kontowährung, sofern verfügbar.
7. Wenn der variable Gewinn `SessionTakeProfitPerLot * OrderVolume` erreicht und `UseSessionTakeProfit` aktiviert ist, wird das gesamte Risiko reduziert. Ebenso löst ein schwebender Verlust unter `-SessionStopLossPerLot * OrderVolume` einen vollständigen Exit aus, wenn `UseSessionStopLoss` aktiviert ist.
8. Manuelle Flags reproduzieren die ursprünglichen EA-Optionen: `CloseNow` erzwingt einen flachen Start, `QuiesceMode` hält die Strategie inaktiv, während sie flach ist, und `StopNow` stoppt neue Eingaben, ohne bestehende Positionen zu beeinträchtigen.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Volumen für jede Marktorder (MT4 `Lots`). |
| `MaxTrades` | Maximale Anzahl gleichzeitig offener Trades (MT4 `maxcount`). |
| `GridStepPoints` | Abstand in Preispunkten zwischen Rasterebenen (MT4 `pipstep`). |
| `QuiesceMode` | Wenn aktiviert, wartet die Strategie einmal flach, identisch mit `quiescenow`. |
| `TriggerImmediateEntries` | Öffnet einen ersten Kauf und Verkauf, sobald die Strategie fertig ist (`donow`). |
| `StopNow` | Pausiert automatische Eingaben und behält gleichzeitig die aktuellen Positionen bei (`stopnow`). |
| `CloseNow` | Fordert eine sofortige Reduzierung beim Start an (`closenow`). |
| `UseSessionTakeProfit` & `SessionTakeProfitPerLot` | Variables Gewinnziel auf Sitzungsebene pro Los. |
| `UseSessionStopLoss` & `SessionStopLossPerLot` | Floating-Loss-Grenzwert auf Sitzungsebene pro Los. |
| `UseInitialValues`, `InitialMax`, `InitialMin` | Optionale Neustartunterstützung, die vorherige Extreme (`useinvalues`, `inmax`, `inmin`) wiederverwendet. |

## Implementierungshinweise
- Der gesamte interne Status wird durch Tabulatoren eingerückt und in Feldern statt in Sammlungen gespeichert, um den Projektrichtlinien zu folgen.
- Marktaufträge werden durch die Verfolgung von `_activeBuyOrder` und `_activeSellOrder` gedrosselt, sodass keine doppelten Anfragen gesendet werden, während die vorherige aussteht.
- Die abgesicherte Bilanzierung wird in `OnOwnTradeReceived` durchgeführt, wobei Long- und Short-Durchschnittspreise/Volumen unabhängig voneinander aktualisiert und für die Stop-Logik in variable Gewinne umgewandelt werden.
- `TryCloseAll()` spiegelt die MT4 `close1by1()`-Routine wider, indem es gegensätzliche Marktaufträge übermittelt, bis beide Ledger flach sind, und dann die Extreme auf den letzten Brief zurücksetzt.
- Die Strategie basiert ausschließlich auf API-Aufrufen auf hoher Ebene (`SubscribeLevel1()`, `BuyMarket`, `SellMarket`) und vermeidet den direkten Indikatorzugriff, wie er in den Repository-Regeln erforderlich ist.
