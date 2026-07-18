# Elderv30aug05v Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Elderv30aug05v-Strategie ist eine direkte Portierung des gleichnamigen MetaTrader 4-Expertenberaters. Es kombiniert Signale von zwei MACD-Filtern, die auf stündlichen Kerzen berechnet werden, und zwei stochastischen Oszillatoren, die auf 15-Minuten-Kerzen berechnet werden. Die Handelsausführung und das Exit-Management erfolgen auf Ein-Minuten-Kerzen, um die Tick-für-Tick-Logik des ursprünglichen MQL-Skripts zu reproduzieren. Die Strategie eröffnet jeweils höchstens eine Position und basiert auf dynamischen Trailing Stops statt auf festen Take-Profit-Orders.

## Indikatoren und Daten
- **Primäre MACD** (`13/30/9`, stündliche Kerzen). Bei einem langen Signal muss das Histogramm ansteigen, während der vorherige Wert unter Null bleibt.
- **Sekundär MACD** (`14/56/9`, stündliche Kerzen). Ein kurzes Signal erfordert, dass das Histogramm abfällt, während der vorherige Wert über Null bleibt.
- **Schneller stochastischer Oszillator** (`%K=2`, `%D=3`, Glättung=3, 15-Minuten-Kerzen). Bei langen Einträgen muss die %K-Linie unter der konfigurierten Obergrenze (Standard 36) liegen und relativ zum vorherigen Balken ansteigen.
- **Langsamer stochastischer Oszillator** (`%K=1`, `%D=3`, Glättung=3, 15-Minuten-Kerzen). Bei Short-Einträgen muss die %K-Linie über der konfigurierten Untergrenze (Standard 66) liegen und relativ zum vorherigen Balken fallen.
- **Einminütige Kerzen** liefern die Bestätigungsdaten für Ausbruchsprüfungen und verwalten Trailing Stops.

Alle Indikatoren verarbeiten nur fertige Kerzen bis `SubscribeCandles().Bind()/BindEx()`, um den übergeordneten StockSharp API-Richtlinien zu folgen.

## Teilnahmebedingungen
### Lange Einrichtung
1. Der primäre MACD-Wert liegt über seinem vorherigen Messwert und der vorherige Messwert ist negativ.
2. Der schnelle stochastische %K liegt unter `LongStochasticThreshold` (Standard 36) und über seinem vorherigen Wert.
3. Der Schlusskurs der aktuellen Ein-Minuten-Kerze liegt über dem Höchststand der vorherigen Ein-Minuten-Kerze.

### Kurze Einrichtung
1. Der sekundäre MACD-Wert liegt unter seinem vorherigen Messwert und der vorherige Messwert ist positiv.
2. Der langsame stochastische %K liegt über `ShortStochasticThreshold` (Standard 66) und unter seinem vorherigen Wert.
3. Der Schlusskurs der aktuellen Ein-Minuten-Kerze liegt niedriger als der Tiefststand der vorherigen Ein-Minuten-Kerze.

Es kann nur eine Position offen sein. Wenn ein neues Signal erscheint, während eine Position aktiv ist, wird es ignoriert, bis die Position durch Stop-Loss oder Trailing-Logik geschlossen wird.

## Ausgangsregeln
- **Anfänglicher Stop-Loss**: Beim Einstieg speichert die Strategie den Einstiegspreis plus/minus `LongStopLoss` oder `ShortStopLoss` multipliziert mit dem Instrument `PriceStep`. Wenn `PriceStep` nicht angegeben ist, wird ein Fallback von `0.0001` verwendet.
- **Trailing Stop**: Sobald sich der Preis um mindestens `LongTrailingStop` oder `ShortTrailingStop` Punkte (wiederum multipliziert mit `PriceStep`) zugunsten des Handels bewegt, wird der gespeicherte Stop-Preis hinter den Markt verschoben. Bei Long-Trades folgt der Stop dem Schlusskurs minus der Trailing-Distanz und bewegt sich nur nach oben. Bei Short-Trades folgt der Stop dem Schlusskurs plus der Distanz und bewegt sich nur nach unten.
- Wenn die Kerzenspanne den gespeicherten Stop-Preis berührt, wird die Position zum Marktwert geschlossen.

Es wird kein festes Take-Profit-Niveau verwendet, was das ursprüngliche MetaTrader-Verhalten widerspiegelt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `Volume` | `0.1` | Handelsvolumen gesendet an `BuyMarket`/`SellMarket`. |
| `LongStopLoss` | `17` | Lange Stop-Loss-Distanz in Punkten. |
| `ShortStopLoss` | `46` | Kurze Stop-Loss-Distanz in Punkten. |
| `LongTrailingStop` | `18` | Nachlaufdistanz für Long-Positionen. |
| `ShortTrailingStop` | `22` | Nachlaufdistanz für Short-Positionen. |
| `LongStochasticThreshold` | `36` | Maximaler schneller stochastischer %K-Wert für lange Einträge. |
| `ShortStochasticThreshold` | `66` | Minimaler langsamer stochastischer %K-Wert für kurze Einträge. |
| `BaseCandleType` | `TimeFrame(1m)` | Kerzenserie, die für die Ausführungslogik verwendet wird. |
| `StochasticCandleType` | `TimeFrame(15m)` | Kerzenreihe für beide stochastischen Oszillatoren. |
| `MacdCandleType` | `TimeFrame(1h)` | Kerzenserie für beide MACD-Filter. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | `13 / 30 / 9` | Zeiträume für den primären MACD. |
| `AltMacdFastPeriod` / `AltMacdSlowPeriod` / `AltMacdSignalPeriod` | `14 / 56 / 9` | Perioden für die sekundäre MACD. |
| `StochasticFastKPeriod` / `StochasticFastDPeriod` / `StochasticFastSmooth` | `2 / 3 / 3` | Parameter für die schnelle Stochastik. |
| `StochasticSlowKPeriod` / `StochasticSlowDPeriod` / `StochasticSlowSmooth` | `1 / 3 / 3` | Parameter für die langsame Stochastik. |

## Notizen
- Die Strategie funktioniert mit jedem Instrument, das Kerzen auf Minutenebene und einen gültigen `PriceStep` bereitstellt.
- Trailing Stops werden intern verwaltet; Börsenseitig werden keine Schutzanordnungen registriert.
- Die Logik verarbeitet nur fertige Kerzen, um ein Neuzeichnen zu vermeiden, und stimmt mit der MQL-Implementierung überein, die auf abgeschlossenen Balken basiert.

## Originalskript
- **Quelle**: `MQL/7674/Elderv30aug05v.mq4`
- **Plattform**: MetaTrader 4 Fachberater.
