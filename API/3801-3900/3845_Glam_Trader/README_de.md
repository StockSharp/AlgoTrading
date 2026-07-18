# Glam Trader (Mehrzeitrahmen-Bestätigung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Strategie repliziert den ursprünglichen MetaTrader „GLAM Trader“-Expertenberater, indem sie Informationen aus drei Zeitrahmen kombiniert:

- Ein schneller **EMA(3)** auf dem 15-Minuten-Chart erfasst kurzfristige Trendverzerrungen.
- Ein **Laguerre-Filter** mit Gamma 0,7, angewendet auf 5-Minuten-Kerzen, misst, ob der Preis über oder unter seinem geglätteten Pfad handelt.
- Der **Awesome Oscillator** auf stündlichen Kerzen liefert einen Momentum-Check, der der Definition von Bill Williams entspricht.

Erst wenn alle drei Komponenten übereinstimmen, eröffnet die Strategie einen Handel mit dem Ziel, Störungen herauszufiltern, die auftreten würden, wenn ein einzelner Zeitrahmen isoliert bewertet würde.

## Handelslogik
1. **Datenvorbereitung**
   - 15-Minuten-Kerzen füttern ein `ExponentialMovingAverage` mit der Länge `EmaPeriod` (Standard 3).
   - 5-Minuten-Kerzen speisen ein `LaguerreFilter` mit glättendem `LaguerreGamma`.
   - 60-Minuten-Kerzen speisen einen `AwesomeOscillator`.
   - Für jeden Zeitrahmen wird der letzte abgeschlossene Kerzenschluss gespeichert, um den ursprünglichen Indikator-Preis-Vergleich zu reproduzieren.
2. **Eintrittsbedingungen**
   - **Long**: Der EMA liegt über dem aktuellen 15-Minuten-Schluss, Laguerre liegt über dem letzten 5-Minuten-Schluss und Awesome Oscillator liegt über dem letzten Stunden-Schluss.
   - **Short**: Jeder der drei Indikatoren muss unter seinem entsprechenden Schlusskurs liegen.
3. **Risikomanagement**
   - Separate Stop-Loss- und Take-Profit-Abstände (ausgedrückt in Instrumentenpunkten) für Long- und Short-Trades.
   - Trailing-Stops werden aktiviert, sobald der Preis mindestens die angegebene Trailing-Distanz über den Einstiegspreis hinausgeht. Der Stopp wird in Trendrichtung gesetzt, ohne nachzugeben.
   - Alle Schutzmaßnahmen (Take-Profit, Stop-Loss, Trailing Stop) schließen die gesamte Position mit Marktaufträgen und spiegeln die MQL-Implementierung wider.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Auftragsgröße für neue Positionen. | 0,1 |
| `PrimaryCandleType` | Zeitrahmen, der für das EMA und das Hauptsignal verwendet wird. | 15-Minuten-Kerzen |
| `LaguerreCandleType` | Mit dem Laguerre-Filter analysierter Zeitrahmen. | 5-Minuten-Kerzen |
| `AwesomeCandleType` | Vom Awesome Oscillator analysierter Zeitrahmen. | 60-Minuten-Kerzen |
| `EmaPeriod` | EMA Länge im primären Zeitrahmen. | 3 |
| `LaguerreGamma` | Gamma-Parameter für den Laguerre-Filter. | 0,7 |
| `LongStopLossPoints` | Stop-Loss-Distanz für Long-Trades, in Punkten. | 20 |
| `ShortStopLossPoints` | Stop-Loss-Distanz für Short-Trades, in Punkten. | 20 |
| `LongTakeProfitPoints` | Take-Profit-Distanz für Long-Trades, in Punkten. | 50 |
| `ShortTakeProfitPoints` | Take-Profit-Distanz für Short-Trades, in Punkten. | 50 |
| `LongTrailingPoints` | Nachlaufdistanz für Long-Trades, in Punkten. | 15 |
| `ShortTrailingPoints` | Nachlaufdistanz für Short-Trades, in Punkten. | 15 |

## Notizen
- Die Strategie abonniert drei unabhängige Kerzenströme und behält nur die aktuellsten Endwerte bei, wodurch manuelle Verlaufspuffer vermieden werden.
- Alle Kommentare und Protokollmeldungen bleiben aus Gründen der Übersichtlichkeit und entsprechend den Projektkonventionen auf Englisch.
- Passen Sie die punktbasierten Risikoparameter entsprechend dem `PriceStep` des Instruments an, sodass die Schutzniveaus die Tick-Größe des Brokers widerspiegeln.
