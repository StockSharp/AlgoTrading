# EA Vishal EURGBP H4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **EA Vishal EURGBP H4-Strategie** repliziert den ursprünglichen MetaTrader-Expertenberater, der einen Stochastik-Kreuzungs-Einstiegsfilter mit hüllenbasierten Ausstiegen kombiniert. Die Logik operiert standardmäßig auf H4-Kerzen und verwendet virtuelle Risikomanagement-Tools (Stop-Loss, Take-Profit und optionaler Trailing Stop) in Pips definiert, was das MT4-Verhalten genau widerspiegelt.

## Handelslogik
- **Einstieg** – die Strategie wartet auf eine Stochastik-Kreuzung, die auf den zwei zuletzt abgeschlossenen Kerzen ausgewertet wird. Eine Long-Position wird eröffnet wenn %K zwischen Bar *n-2* und *n-1* unter %D kreuzt. Eine Short-Position wird bei der umgekehrten Kreuzung eröffnet. Nur eine Position kann gleichzeitig aktiv sein.
- **Ausstieg** – aktive Positionen werden in drei Schichten verwaltet:
  1. **Hüllen-Ausbruch** – wenn die nächste Bar jenseits des vorherigen Hüllenbandes öffnet während die frühere Bar innerhalb geöffnet hat, wird die Position sofort geschlossen.
  2. **Virtueller Stop-Loss / Take-Profit** – Zielpreise werden aus dem Einstiegspreis mithilfe der konfigurierten Pip-Abstände berechnet.
  3. **Optionaler Trailing Stop** – wenn aktiviert und ein Stop-Loss definiert ist, verfolgt das Stop-Niveau den höchsten (für Longs) oder niedrigsten (für Shorts) Wert der vorherigen Kerze minus/plus der Stop-Distanz.

## Parameter
| Name | Standard | Beschreibung |
| ---- | -------- | ------------ |
| `Volume` | 0.5 | Ordervolumen in Lots für jeden Trade. |
| `StopLossPips` | 0 | Hard-Stop-Loss-Abstand in Pips (0 deaktiviert den Stop). |
| `TakeProfitPips` | 22 | Take-Profit-Abstand in Pips (0 deaktiviert das Ziel). |
| `UseTrailingStop` | false | Aktiviert den virtuellen Trailing Stop, der dem Extremum der vorherigen Kerze folgt. Erfordert `StopLossPips` &gt; 0. |
| `StochasticKPeriod` | 6 | Lookback-Periode für die Stochastik-%K-Berechnung. |
| `StochasticDPeriod` | 3 | Glättungsperiode für die %D-Linie. |
| `StochasticSlowing` | 1 | Verlangsamungsfaktor für %K. |
| `EnvelopePeriod` | 32 | Länge des SMA als Hüllenbasis. |
| `EnvelopeDeviationPercent` | 0.3 | Abweichung in Prozent über/unter dem SMA zum Aufbau der Hüllen. |
| `CandleType` | H4-Zeitrahmen | Kerzen-Serie, die die Strategie speist (Standard: Vier-Stunden-Kerzen). |

## Hinweise
- Alle Parameter sind für die Optimierung in StockSharp Studio freigelegt.
- Schutzniveaus werden intern verfolgt und mit Marktorders ausgeführt wenn der Kerzenbereich sie durchbricht, entsprechend dem Verhalten des ursprünglichen Expertenberaters bei neuen Bar-Ereignissen.
- Die Strategie verlässt sich nur auf abgeschlossene Kerzen, was deterministische Backtests und Produktionsverhalten gewährleistet.
