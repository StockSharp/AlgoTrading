# Spasm-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Zusammenfassung
- Konvertierung des MetaTrader 5-Expertenberaters *Spasm (barabashkakvn's Edition)* zur High-Level-API von StockSharp.
- Handelt Ausbrüche aus einem adaptiven Kanal, der durch die jüngste Volatilität dimensioniert wird, und wechselt zwischen bullischen und bärischen Regimes.
- Funktioniert auf jedem Instrument und Zeitrahmen, der durch den Parameter `CandleType` bereitgestellt wird, standardmäßig Stundenkerzen.

## Datenvorbereitung
1. Abonniert die durch `CandleType` definierte Kerzenserie für das Strategie-Wertpapier.
2. Erstellt einen Volatilitätsschätzer aus den letzten `VolatilityPeriod` Kerzen:
   - Wenn `UseWeightedVolatility` deaktiviert ist, ist der Schätzer ein einfacher gleitender Durchschnitt der Kerzenspanne.
   - Wenn `UseWeightedVolatility` aktiviert ist, wird der Schätzer zu einem linear gewichteten gleitenden Durchschnitt, der die neuesten Balken betont.
3. Die Kerzenspanne ist standardmäßig `High - Low`. Wenn `UseOpenCloseRange` aktiviert ist, wird stattdessen die absolute Differenz zwischen Eröffnung und Schluss verwendet, was den Moduswechsel des Original-EA reproduziert.
4. Der rohe Durchschnittskurs wird in Preisschritte umgerechnet und mit `VolatilityMultiplier` multipliziert. Das Ergebnis wird auf eine ganzzahlige Anzahl von Schritten abgerundet und schließlich wieder mit der Tick-Größe des Instruments multipliziert, um den Ausbruchsschwellenwert zu bilden.
5. Während der ersten `VolatilityPeriod * 3` abgeschlossenen Kerzen sammelt die Strategie das neueste höchste Hoch und niedrigste Tief zusammen mit ihren Zeitstempeln, um zu entscheiden, welcher Swing aktueller ist. Diese Information initialisiert den anfänglichen Trendzustand und die Referenzpreise, sobald genügend Kerzen verarbeitet wurden.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `1` | Ordervolumen bei jedem Markteinstieg. |
| `VolatilityMultiplier` | `5` | Multiplikator für die gemittelte Volatilität zur Dimensionierung des Ausbruchspuffers. |
| `VolatilityPeriod` | `24` | Anzahl der Kerzen für die Volatilitätsmittelung und den anfänglichen Swing-Scan. |
| `UseWeightedVolatility` | `false` | Wechselt den Volatilitätsdurchschnitt von einfachem zu linear gewichtetem gleitenden Durchschnitt. |
| `UseOpenCloseRange` | `false` | Verwendet die absolute Eröffnungs-Schluss-Bewegung als Volatilitätsquelle anstatt der Hoch-Tief-Spanne. |
| `StopLossFraction` | `0.5` | Anteil des Volatilitätsschwellenwerts zur Berechnung des Stop-Loss-Abstands. Ein Minimum von drei Preisschritten wird erzwungen. |
| `CandleType` | `1-Stunden-Zeitrahmen` | Kerzentyp und Zeitrahmen für alle Berechnungen. |

## Handelslogik
1. **Trend-Tracking**
   - Die Strategie hält `_highestPrice` und `_lowestPrice` als Anker des aktuellen Swings.
   - Wenn der Preis um mehr als den aktuellen Schwellenwert über das gespeicherte Hoch hinaus steigt, wird `_highestPrice` auf das Kerzenhoch aktualisiert. Analog dazu aktualisiert ein Rückgang jenseits des Schwellenwerts `_lowestPrice` auf das Kerzentief.
   - Das boolesche `_isTrendUp` speichert, ob sich die Strategie derzeit im bullischen (true) oder bärischen (false) Regime befindet.
2. **Einstiegsregeln**
   - Wenn `_isTrendUp` `false` ist (bärisches Regime) und der Kerzenschluss `_lowestPrice + threshold` überschreitet, wechselt die Strategie in den bullischen Modus und sendet `BuyMarket(Volume + Math.Abs(Position))`. Dies schließt jegliches Short-Exposure und eröffnet eine Long-Position gleich `Volume`.
   - Wenn `_isTrendUp` `true` ist (bullisches Regime) und der Kerzenschluss unter `_highestPrice - threshold` fällt, wechselt die Strategie in den bärischen Modus und sendet `SellMarket(Volume + Math.Abs(Position))`, um in eine Short-Position zu wechseln.
3. **Stop-Management**
   - Beim Eintritt in eine Long-Position wird der Stop-Preis bei `entry - max(threshold * StopLossFraction, 3 * priceStep)` platziert.
   - Beim Eintritt in eine Short-Position wird der Stop-Preis bei `entry + max(threshold * StopLossFraction, 3 * priceStep)` platziert.
   - Wenn das Tief einer Kerze den Long-Stop erreicht oder das Hoch den Short-Stop erreicht, wird die entsprechende Position durch eine Marktorder geschlossen. Stops sind deaktiviert, wenn `StopLossFraction` auf null gesetzt ist.
4. **Risikokontrollen und Infrastruktur**
   - `StartProtection()` wird beim Start aufgerufen, sodass die integrierten Risikoprotektionen aktiviert werden, sobald die Strategie startet.
   - Die Strategie reagiert nur auf abgeschlossene Kerzen, um Intrabar-Rauschen zu vermeiden und die balkenweise Neuberechnung des Original-EA zu spiegeln.
   - Alle Kommentare und Parameternamen werden gemäß den Anforderungen auf Englisch gehalten.

## Unterschiede zur MQL-Version
- Der ursprüngliche EA berechnete Schwellenwerte bei jedem Tick neu. In diesem Port wird die Logik auf abgeschlossenen Kerzen ausgeführt, da die High-Level-API mit Kerzenabonnements arbeitet.
- Stop-Loss-Durchsetzung erfolgt auf Kerzendaten. Intrabar-Stop-Treffer, die sich innerhalb derselben Kerze umkehren, werden daher an den Kerzengrenzen ausgewertet.
- Symbol-Eigenschaften wie Spread und broker-spezifische Stop-Level sind in StockSharp nicht in der gleichen Form verfügbar. Ein konservatives Minimum von drei Preisschritten wird verwendet, wenn der berechnete Stop-Abstand zu klein ist, was den Fallback der MetaTrader-Implementierung reproduziert.

## Verwendungshinweise
- Stellen Sie sicher, dass das Strategie-Wertpapier einen gültigen `PriceStep` liefert. Wenn er nicht angegeben ist, setzt der Code den Schritt standardmäßig auf `1`.
- Die Strategie ist richtungsagnostisch und kann auf Spot-, Futures- oder CFD-Instrumenten verwendet werden, solange der Feed die konfigurierten Kerzen liefert.
- Es ist kein Take-Profit-Ziel definiert; Ausstiege erfolgen nur über Regimewechsel oder Stop-Loss-Auslöser.
