# Parabolic SAR First-Dot-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Parabolic SAR First Dot Strategy** ist die StockSharp High-Level-Konvertierung des MetaTrader Expert Advisors `pSAR_bug_4` aus dem Ordner `MQL/9954`. Das System reagiert auf den allerersten Punkt des Parabolic SAR, der auf der gegenüberliegenden Seite des Preises erscheint. Wenn der SAR unter den Schlusskurs fällt, wird ein Long-Trade eröffnet; Wenn der SAR über den Schlusskurs springt, wird ein Short-Trade ausgeführt. Jede Position ist durch feste Stop-Loss- und Take-Profit-Abstände geschützt, ausgedrückt in Parabolic SAR „Punkten“, genau wie in der ursprünglichen MQL-Version.

## Handelslogik
1. **Daten- und Indikatorvorbereitung**. Die Strategie abonniert einen konfigurierbaren Kerzentyp (standardmäßig 15-Minuten-Kerzen) und bindet einen Parabolic SAR-Indikator mit benutzerdefiniertem Beschleunigungsschritt und maximaler Beschleunigung.
2. **Statusverfolgung**. Bei der ersten abgeschlossenen Kerze merkt sich die Strategie, ob der SAR über oder unter dem Schlusskurs liegt. Spätere Kerzen vergleichen die neue SAR-Position mit dem vorherigen Zustand.
3. **Eintrittsregeln**.
   - **Langer Einstieg**: Der SAR wechselt von über dem Schlusskurs zu unter dem Schlusskurs. Eine eventuell bestehende Short-Position wird geschlossen und eine neue Long-Position mit dem konfigurierten Volumen zum Marktwert eröffnet.
   - **Kurzer Einstieg**: Der SAR wechselt von unter dem Schlusskurs auf über dem Schlusskurs. Eine bestehende Long-Position wird geschlossen, bevor eine neue Short-Position eröffnet wird.
4. **Schutzanordnungen**. Unmittelbar nach dem Einstieg speichert die Strategie Stop-Loss- und Take-Profit-Level, die aus dem Kerzenschluss berechnet werden, indem `StopLossPoints` oder `TakeProfitPoints` mit dem Wertpapier `PriceStep` multipliziert wird. Wenn `UseStopMultiplier` aktiviert ist (Standardverhalten, kopiert von MetaTrader), wird die Distanz mit 10 multipliziert, um Brokern Rechnung zu tragen, die mit gebrochenen Pips quotieren.
5. **Ausgangsregeln**. Bei jeder fertigen Kerze vergleicht die Strategie die Höchst- und Tiefststände mit den gespeicherten Stop-Loss- und Take-Profit-Werten. Wenn das Hoch oder Tief das Niveau durchbricht, wird die Position zum Marktwert geschlossen. Wenn ein entgegengesetztes SAR-Signal eintrifft, wird die Position ebenfalls umgekehrt, indem ein Auftrag gesendet wird, der so dimensioniert ist, dass das aktuelle Risiko geglättet und der neue Handel eröffnet wird.

## Risikomanagement
- Stop-Loss- und Take-Profit-Abstände werden für jede neue Position neu berechnet.
- Der Code führt einen konservativen Fallback durch: Wenn das Wertpapier keinen Preisschritt bereitstellt, wird ein Wert von `0.0001` verwendet, um Nullabstände zu vermeiden.
- Bei allen Handelsentscheidungen wird der `IsFormedAndOnlineAndAllowTrading()`-Helfer verwendet, um sicherzustellen, dass das Abonnement aktiv und live ist.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Auftragsvolumen, das für neue Positionen verwendet wird. Der Parameter aktualisiert auch die Basiseigenschaft `Strategy.Volume`. |
| `StopLossPoints` | `90` | Stop-Loss-Distanz, ausgedrückt in Parabolic SAR Punkten. Der Wert wird mit der Sicherheit `PriceStep` multipliziert (und optional mit 10, wenn `UseStopMultiplier` wahr ist). |
| `TakeProfitPoints` | `20` | Take-Profit-Distanz in Parabolic SAR Punkten, umgewandelt durch den Preisschritt. |
| `UseStopMultiplier` | `true` | Wenn aktiviert, werden die Stop-Loss- und Take-Profit-Distanzen mit 10 multipliziert, um den `StopMult`-Schalter des MetaTrader-Experten nachzuahmen. |
| `SarAccelerationStep` | `0.02` | Anfänglicher Beschleunigungsfaktor, der dem Indikator Parabolic SAR zugeführt wird. |
| `SarAccelerationMax` | `0.2` | Maximaler Beschleunigungsfaktor für den Indikator Parabolic SAR. |
| `CandleType` | `15m time-frame` | Kerzentyp, der für die Indikator- und Signalberechnungen verwendet wird. |

## Hinweise zur Konvertierung
- MetaTrader Stop-Loss- und Take-Profit-Orders waren Broker-seitige Schutzorder. StockSharp reproduziert sie, indem es Kerzenhochs und -tiefs überwacht und Marktausgänge sendet, wenn die Schwellenwerte überschritten werden.
- Der MetaTrader-Experte multiplizierte die Stop-Distanzen mit zehn, wenn `StopMult` wahr war, um die Kompatibilität mit Brokern zu verbessern, die mit gebrochenen Pips quotieren. Der Parameter `UseStopMultiplier` implementiert das gleiche Verhalten.
- Bei der Konvertierung werden die übergeordneten API von StockSharp (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`) gemäß den Projektrichtlinien verwendet. Es wird noch keine zusätzliche Python-Version bereitgestellt, die der Aufgabenanforderung entspricht.
