# Macd Stochastic Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

# MACD Stochastic Nachlaufende Strategie

## Überblick
- Konvertiert vom MetaTrader 4 Expert Advisor `MQL/7637/3_lccfpgubwykd__www_forex-instruments_info.mq4`.
- Verwendet einen Workflow mit **drei Zeitrahmen**: Stündliche Kerzen steuern beide MACD-Filter, 15-Minuten-Kerzen liefern Stochastic-Oszillatoren und 1-Minuten-Kerzen bestätigen Preisausbrüche und verwalten Trailing-Exits.
- Implementiert eine übergeordnete StockSharp-Strategie mit `SubscribeCandles(...).Bind(...)` / `BindEx(...)` ohne manuelle Datenabfrage.
- Positionen werden mit Marktaufträgen eröffnet und vollständig innerhalb der Strategie verwaltet (es waren keine externen Test-Änderungen erforderlich).

## Indikatoren und Parameter
| Name | Typ | Standard | Beschreibung |
| ---- | ---- | ------- | ----------- |
| `LongStopLoss` | `decimal` | `17` | Anfänglicher Stoppabstand für Long-Trades, ausgedrückt in Instrumentenpunkten. |
| `ShortStopLoss` | `decimal` | `40` | Anfängliche Stoppdistanz für Short-Trades (Punkte). |
| `LongTrailingStop` | `decimal` | `88` | Nachlaufdistanz für Long-Positionen. |
| `ShortTrailingStop` | `decimal` | `76` | Nachlaufdistanz für Short-Positionen. |
| `OrderVolume` | `decimal` | `0.1` | Basishandelsvolumen (Lots), gespiegelt aus der Eingabe MQL. |
| `MacdCandleType` | `DataType` | `H1` | Zeitrahmen für die bullischen und bärischen MACD-Filter (`22/27/9` und `19/77/9`). |
| `StochasticCandleType` | `DataType` | `M15` | Zeitrahmen, der für beide Stochastic-Oszillatoren (`5/3/11` und `9/3/19`) verwendet wird. |
| `EntryCandleType` | `DataType` | `M1` | Zeitrahmen, der eine Ausbruchsbestätigung und eine abschließende Logik bereitstellt. |

Alle punktbasierten Einstellungen werden durch das Instrument `PriceStep` in absolute Preise umgewandelt, wodurch der Multiplikator MetaTrader `Point` originalgetreu reproduziert wird.

## Handelsregeln
### Langer Eintrag
1. Die stündliche Hauptlinie MACD(22,27,9) überschreitet ihren vorherigen Wert, bleibt aber unter Null.
2. M15 Stochastic(%K=5,%D=3,slowing=11) liegt unter 26 und steigt im Vergleich zum vorherigen Wert.
3. Der aktuelle M1-Schluss durchbricht das vorherige M1-Hoch.
4. Wenn alle Bedingungen übereinstimmen und keine Position offen ist, kauft die Strategie `OrderVolume` plus die Menge, die zum Umtausch eines bestehenden Short erforderlich ist.

### Kurzer Eintrag
1. Die stündliche MACD(19,77,9) Hauptlinie fällt unter ihren vorherigen Wert, wobei der vorherige Wert über Null liegt.
2. M15 Stochastic(%K=9,%D=3,slowing=19) liegt über 70.
3. Der aktuelle M1-Schlusskurs bricht unter das vorherige M1-Tief.
4. Ein Short wird mit der gleichen Positions-Flip-Logik wie der ursprüngliche EA eröffnet.

### Exit und Trailing
- Die anfänglichen Stopps spiegeln die Entfernungen von MQL `StopLoss` in Punkten wider.
- Trailing-Stops werden aktiviert, sobald sich der Preis um mehr als die angegebene Trailing-Distanz zugunsten der Position bewegt, und werden bei jeder beendeten M1-Kerze neu berechnet.
- Wenn der Preis das aktive Stop-Level (Anfangs- oder Trailed-Stop-Level) berührt, wird die Position mit einer Marktorder geschlossen.

## Implementierungshinweise
- Kerzenabonnements werden nach Zeitrahmen aufgeteilt, sodass Indikatoraktualisierungen unabhängig bleiben und genau dem Verhalten von EA in mehreren Zeitrahmen entsprechen.
- Die nachgestellten Vergleiche MQL `Bid`/`Ask` werden durch die fertigen M1-Kerzenhöchst-/-tiefs angenähert, was die nächstliegende Darstellung innerhalb des kerzenbasierten Hochniveaus API darstellt.
- Der Code folgt den Repository-Richtlinien: Tab-Einrückung, Namespace `StockSharp.Samples.Strategies`, englische Kommentare und Parameterdeklarationen innerhalb des Konstruktors über `Param(...)`.
