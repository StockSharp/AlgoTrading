# Neuro Nirvaman MQ4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Neuro Nirvaman MQ4-Strategie** ist eine originalgetreue Umsetzung des MetaTrader 4-Expertenberaters `NeuroNirvaman.mq4`. Der ursprüngliche Roboter kombiniert einen benutzerdefinierten Laguerre-Filter, der auf die +DI-Komponente des ADX-Indikators angewendet wird, mit einem SilverTrend-Breakout-Detektor. Drei Perzeptrone werten diese Eingaben aus und ein Supervisor entscheidet über Kauf oder Verkauf. Die StockSharp-Version spiegelt diesen Workflow wider und führt jeweils eine Position aus, wobei ihre Logik nur bei vollständig geschlossenen Kerzen neu berechnet wird.

## Wie die Strategie funktioniert
1. **Marktdaten-Feed** – Die Strategie abonniert eine einzelne Kerzenserie, die durch `CandleType` definiert wird, und verarbeitet nur `Finished` Kerzen. Es wertet keine Intrabar-Ereignisse aus und repliziert die in MT4 verwendete `Time[0]`-Prüfung.
2. **Laguerre +DI-Glättung** – Vier `AverageDirectionalIndex`-Indikatoren liefern +DI-Werte, die unter Verwendung des ursprünglichen Gammas von 0,764 durch einen Laguerre-Filter (`LaguerrePlusDiState`) gesendet werden. Der Filter liefert Oszillatorwerte im `[0, 1]`-Bereich und jeder Strom hat seine eigene ADX-Periode und neutrale Zonenbreite (`Laguerre*Distance`).
3. **SilverTrend-Port** – Zwei `SilverTrendState`-Objekte reproduzieren die `Sv2.mq4`-Logik. Sie verfolgen das höchste Hoch und das niedrigste Tief für `SSP`-Kerzen, verkleinern den Kanal mit der Konstante `Kmax = 50.6` und geben `1` für einen Aufwärtstrend oder `-1` für einen Abwärtstrend zurück. Die Lookback-Tiefen werden durch `SilverTrend1Length` und `SilverTrend2Length` gesteuert.
4. **Perzeptrone** –
   - *Perceptron #1* mischt die erste Laguerre-Aktivierung mit dem ersten SilverTrend-Schwung unter Verwendung der Gewichte `X11 - 100` und `X12 - 100`.
   - *Perceptron #2* kombiniert die zweite Laguerre-Aktivierung mit dem zweiten SilverTrend-Schwung und den Gewichten `X21 - 100` und `X22 - 100`.
   - *Perceptron #3* wertet die dritte und vierte Laguerre-Aktivierung gewichtet mit `X31 - 100` und `X32 - 100` aus.
Jede Laguerre-Aktivierung wird je nach Abstand vom 0,5-Gleichgewichtsniveau zu `-1`, `0` oder `1` quantisiert.
5. **Supervisor (`Pass`)** – Der Supervisor reproduziert die Funktion MQL `Supervisor()`:
   - `Pass = 3`: erfordert `Perceptron #3 > 0`. Wenn auch `Perceptron #2 > 0`, kauft die Strategie mit dem zweiten TP/SL-Satz; andernfalls, wenn `Perceptron #1 < 0`, wird mit dem ersten TP/SL-Set verkauft.
   - `Pass = 2`: Ein positiver `Perceptron #2` eröffnet eine Long-Position mit dem zweiten TP/SL-Satz, während jeder nicht positive Wert eine Short-Position mit dem ersten Satz eröffnet.
   - `Pass = 1`: Ein negativer `Perceptron #1` eröffnet einen Short, andernfalls wird ein Long eröffnet. Beide Zweige verwenden den ersten TP/SL-Satz.
6. **Auftrags- und Risikomanagement** – Einträge werden mit `BuyMarket` oder `SellMarket` mit `TradeVolume` gesendet. Take-Profit- und Stop-Loss-Level werden als `entry ± points * PriceStep` berechnet. Da StockSharp reine Marktaufträge platziert, werden Schutzausstiege durch die Überprüfung von Kerzenhochs und -tiefs simuliert, genau wie TP/SL-Aufträge auf Brokerseite in MT4 ausgelöst würden.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 15-minütiger Zeitrahmen | Von der Strategie verarbeiteter Kerzentyp. |
| `TradeVolume` | `decimal` | 0,1 | Bestellvolumen in Losen. |
| `SilverTrend1Length` | `int` | 7 | Lookback-Länge für die erste SilverTrend-Berechnung (SSP). |
| `Laguerre1Period` | `int` | 14 | ADX Zeitraum für den ersten Laguerre-Stream. |
| `Laguerre1Distance` | `decimal` | 0 | Die Breite der neutralen Zone (Prozent) beträgt etwa 0,5 für den Laguerre-Strom Nr. 1. |
| `X11`, `X12` | `decimal` | 100 | Gewichte, die im Perzeptron Nr. 1 verwendet werden (der Code subtrahiert 100, bevor er sie anwendet). |
| `TakeProfit1`, `StopLoss1` | `decimal` | 100 / 50 | Schutzabstände in Punkten für das erste Risikoprofil und alle Short-Trades. |
| `SilverTrend2Length` | `int` | 7 | Lookback-Länge für die zweite SilverTrend-Berechnung. |
| `Laguerre2Period` | `int` | 14 | ADX Zeitraum für den zweiten Laguerre-Stream. |
| `Laguerre2Distance` | `decimal` | 0 | Die Breite der neutralen Zone (Prozent) beträgt etwa 0,5 für den Laguerre-Strom Nr. 2. |
| `X21`, `X22` | `decimal` | 100 | Gewichte, die im Perzeptron Nr. 2 verwendet werden. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 100 / 50 | Schutzabstände in Punkten für das zweite Risikoprofil. |
| `Laguerre3Period`, `Laguerre4Period` | `int` | 14 | ADX Perioden für den dritten und vierten Laguerre-Strom. |
| `Laguerre3Distance`, `Laguerre4Distance` | `decimal` | 0 | Breiten der neutralen Zone (Prozent) für den dritten und vierten Laguerre-Strom. |
| `X31`, `X32` | `decimal` | 100 | Gewichte, die im Perzeptron Nr. 3 verwendet werden. |
| `Pass` | `int` | 3 | Supervisor-Zweig, der auswählt, welche Perzeptrone Trades auslösen können. |

## Nutzungshinweise
- Standardgewichte von `100` neutralisieren die entsprechende Perzeptron-Eingabe. Bewegen Sie die Gewichte von 100 weg, um aussagekräftige Signale zu erzeugen.
- SilverTrend beginnt mit der Rückgabe von `±1`, sobald genügend Kerzen gesammelt wurden. Bis dahin können die Perceptron-Ausgaben auf Null bleiben und das MT4-Verhalten nachahmen, bei dem `iCustom` Null zurückgibt, bevor die Puffer bereit sind.
- Take-Profit- und Stop-Loss-Checks basieren auf Candle-Extremen; Wenn zwischen den Balken Intra-Candle-Spitzen auftreten, kann die Simulation leicht von der Ausführung auf Brokerseite abweichen.
- Es kann jeweils nur eine Position existieren. Ein neues Signal wird ignoriert, bis die aktuelle Position entweder durch TP, SL oder eine gegenteilige Entscheidung geschlossen wird.
- Passen Sie `CandleType` an, um den vom ursprünglichen MT4-Setup verwendeten Diagrammzeitraum (z. B. M15 oder H1) widerzuspiegeln, um die Skalierung des Indikators konsistent zu halten.
