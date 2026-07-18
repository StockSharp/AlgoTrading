# Williams-AO-+-AC-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Williams-AO-+-AC-Strategie** wandelt den MetaTrader-4 Expert "Williams_AOAC" in die High-Level-Strategie-API von StockSharp um. Der Ansatz kombiniert mehrere Bill-Williams-Werkzeuge, um Momentum-Schübe auf dem Stundenchart (Standardzeitrahmen) zu finden:

1. **Bollinger-Band-Filter** - die Strategie handelt nur, wenn die Bandbreite innerhalb eines konfigurierbaren Punktebereichs liegt, was sowohl flache Märkte als auch übermäßige Volatilität vermeidet.
2. **Relative-Strength-Index-Bestätigung** - der RSI muss für Longs über einem bullischen Schwellenwert oder für Shorts unter einem bärischen Schwellenwert liegen.
3. **Awesome-Oscillator-Nulllinienkreuzung** - der Oszillator muss die Nullachse in Handelsrichtung kreuzen und damit eine Momentum-Verschiebung signalisieren.
4. **Accelerator-Oscillator-Beschleunigung** - die letzten drei Accelerator-Werte müssen auf derselben Seite von null liegen, und die jüngste Bar muss diese Bewegung fortsetzen, um Beschleunigung zu bestätigen.
5. **Handelssitzungsfilter** - Einstiege sind nur innerhalb eines konfigurierbaren Zeitfensters erlaubt, das in Tagesstunden ausgedrückt wird.

Auf jeder abgeschlossenen Kerze verarbeitet die Strategie die Indikatorwerte, die von der `Bind`-Pipeline geliefert werden. Wenn alle Filter übereinstimmen, schließt sie bei Bedarf eine Gegenposition und eröffnet eine neue Marktorder mit der angeforderten Lotgröße. Stop-Loss und Take-Profit werden über Distanzen in Preispunkten angewendet, und ein optionaler Trailing Stop kann den Schutz-Stop enger ziehen, nachdem der Trade profitabel wird.

## Einstiegsregeln
### Long-Bedingungen
1. Bollinger-Spread (oberes minus unteres Band, in Punkte umgerechnet) liegt zwischen **BollingerSpreadLower** und **BollingerSpreadUpper**.
2. Der RSI-Wert ist strikt größer als **RsiBuyThreshold**.
3. Awesome Oscillator kreuzt auf der aktuellen Bar von negativ nach positiv.
4. Accelerator-Oscillator-Werte der letzten drei Kerzen sind alle positiv, und der jüngste Wert ist höher als der vorherige, was wachsendes bullisches Momentum signalisiert.
5. Die Eröffnungszeit der aktuellen Bar liegt innerhalb des Handelsfensters, das bei **EntryHour** beginnt und sich über **TradingWindowHours** Stunden erstreckt (bei Bedarf über Mitternacht hinweg).
6. Die Strategie hält noch keine Long-Position (sie kann flat oder short sein).

Wenn die Logik erfüllt ist, schließt die Strategie jede Short-Exposure, eröffnet eine Long-Marktorder mit **TradeVolume** und wendet die konfigurierten Stop-Loss-/Take-Profit-Distanzen an. Trailing-Stop-Verfolgung beginnt, nachdem sich der Trade um mindestens **TrailingStopPoints** zugunsten der Position bewegt hat.

### Short-Bedingungen
1. Bollinger-Spread liegt innerhalb des erlaubten Bereichs.
2. Der RSI-Wert ist strikt kleiner als **RsiSellThreshold**.
3. Awesome Oscillator kreuzt auf der aktuellen Bar von positiv nach negativ.
4. Accelerator-Oscillator-Werte der letzten drei Kerzen sind alle negativ, und der jüngste Wert ist niedriger als der vorherige, was steigenden bärischen Druck anzeigt.
5. Die Kerzeneröffnungszeit liegt innerhalb des Handelssitzungsfensters.
6. Die Strategie hält noch keine Short-Position (sie kann flat oder long sein).

Wenn ausgelöst, schließt das Modul Long-Exposure, steigt mit **TradeVolume** in eine Short-Marktorder ein und weist die Schutzorders neu zu.

## Ausstiegsverwaltung
* **Take-Profit** - wenn **TakeProfitPoints** größer als null ist, wird an jede neue Position ein Gewinnziel angehängt, das so viele Preispunkte vom Einstiegspreis entfernt liegt.
* **Stop-Loss** - wenn **StopLossPoints** größer als null ist, wird ein fester Stop relativ zum Einstiegspreis angewendet.
* **Trailing Stop** - wenn **TrailingStopPoints** größer als null ist, wird der Stop-Loss näher an den Markt verschoben, sobald der Gewinn die Trailing-Distanz überschreitet. Für Long-Trades wird der Stop auf `Close - TrailingStopPoints * pip` angehoben, für Shorts auf `Close + TrailingStopPoints * pip` abgesenkt. Trailing ist einseitig: Der Stop bewegt sich nie zurück.
* Manuelle Positionsänderungen durch den Benutzer werden respektiert; die Trailing-Logik reagiert auf die aktuelle aggregierte Position, die von der Engine gemeldet wird.

## Parameter
| Name | Beschreibung | Standard |
|------|--------------|----------|
| `CandleType` | Primäre Kerzenserie für Berechnungen. | 1-Stunden-Kerzen |
| `BollingerPeriod` | Rückblickperiode für die Bollinger Bands. | 20 |
| `BollingerDeviation` | Standardabweichungsmultiplikator. | 2.0 |
| `BollingerSpreadLower` | Minimale Bandbreite in Punkten, die zum Aktivieren des Handels erforderlich ist. | 40 |
| `BollingerSpreadUpper` | Maximale für den Handel erlaubte Bandbreite in Punkten. | 210 |
| `AoFastPeriod` | Kurze Periode des Awesome Oscillator. | 11 |
| `AoSlowPeriod` | Lange Periode des Awesome Oscillator. | 40 |
| `RsiPeriod` | RSI-Berechnungslänge. | 20 |
| `RsiBuyThreshold` | Minimaler RSI-Wert für Long-Trades. | 46 |
| `RsiSellThreshold` | Maximaler RSI-Wert für Short-Trades. | 40 |
| `EntryHour` | Stunde (0-23), zu der das Handelsfenster beginnt. | 0 |
| `TradingWindowHours` | Dauer des erlaubten Handelsfensters in Stunden (`0` behält nur die Startstunde). | 20 |
| `TradeVolume` | Lotgröße für jede neue Position. | 0.01 |
| `StopLossPoints` | Stop-Loss-Distanz in Preispunkten. | 60 |
| `TakeProfitPoints` | Take-Profit-Distanz in Preispunkten. | 90 |
| `TrailingStopPoints` | Trailing-Stop-Distanz in Preispunkten. | 30 |

## Zusätzliche Hinweise
* Der Accelerator-Oscillator-Wert wird intern abgeleitet, indem ein einfacher gleitender 5-Perioden-Durchschnitt des Awesome Oscillator vom aktuellen AO-Wert abgezogen wird; dies entspricht der MetaTrader-Implementierung des ursprünglichen Expert.
* Band-Spread-Berechnungen hängen vom Instrumenten-`PriceStep` ab. Wenn er nicht verfügbar ist, fällt die Strategie auf rohe Preisdifferenzen zurück.
* Das Handelssitzungsfenster läuft über Mitternacht, wenn `EntryHour + TradingWindowHours` 23 überschreitet, und reproduziert so den MetaTrader-Stundenfilter.
* Die Strategie schließt automatisch Gegenexposure, bevor sie eine neue Position eröffnet, und repliziert damit das Ein-Order-Limit des ursprünglichen MQL4-Codes.
