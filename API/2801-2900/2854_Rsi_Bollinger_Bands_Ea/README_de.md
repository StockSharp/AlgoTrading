# RSI Bollinger Bands EA (StockSharp-Konvertierung)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Diese Strategie ist ein High-Level-StockSharp-Port des MetaTrader 5-Expertenberaters "RSI Bollinger Bands EA". Sie handelt auf 15-Minuten-Kerzen und kombiniert zwei unabhängige RSI-basierte Auslöser:

* **Auslöser Eins** – feste Überkauft-/Überverkauft-Schwellen für RSI auf M15, H1 und H4 zusammen mit einer stochastischen Bestätigung und einem Steigungsfilter.
* **Auslöser Zwei** – adaptive RSI-Bänder, berechnet aus asymmetrischen Standardabweichungen (getrennte positive/negative Sigma) über konfigurierbare Stichprobengrößen auf allen drei Zeitrahmen. Der RSI muss die dynamischen Bänder durchbrechen, während der Stochastik das Momentum bestätigt.

Beide Auslöser erfordern Volatilitätskontraktionen auf dem niedrigeren Zeitrahmen (M15-Bollinger-Spread), Volatilitätsexpansion auf dem höheren Zeitrahmen (H4-Bollinger-Spread) und eine ruhige Umgebung gemäß dem H4-ATR. Nur ein Auslöser kann gleichzeitig aktiviert sein, was die ursprünglichen EA-Einschränkungen widerspiegelt.

## Datenanforderungen
* Primäre Ausführungskerzen: `M15CandleType` (Standard: 15 Minuten). Alle Einstiege und Ausstiege werden beim Schlusskurs dieser Kerzen bewertet.
* Bestätigungskerzen: `H1CandleType` (Standard: 1 Stunde) für RSI-Bedingungen und Statistiken.
* Höherer Zeitrahmen-Kerzen: `H4CandleType` (Standard: 4 Stunden) für RSI-Prüfungen, Bollinger-Spread-Filter und ATR-Volatilitätsfilter.

## Handelslogik
1. **Session-Filter**
   * Der Handel ist auf ein konfigurierbares Zeitfenster beschränkt, das bei `EntryHour` beginnt und `OpenHours` Stunden dauert. Wenn `OpenHours` null ist, dauert das Fenster für die einzelne Eröffnungsstunde.
   * Der Handel stoppt freitags, sobald die Kerzenstunde `FridayEndHour` erreicht (Standard: 4, d.h. nach 04:00 Uhr freitags).
   * Eine neue Position kann nur eröffnet werden, wenn die aktuelle Nettoposition flach ist (`Position == 0`).

2. **Volatilitäts- und Spread-Filter (beide Auslöser)**
   * Der H4-Bollinger-Spread muss größer als `BbSpreadH4MinX` Pips sein (X = 1 oder 2), um sicherzustellen, dass die Spanne des höheren Zeitrahmens breit genug ist.
   * Der M15-Bollinger-Spread muss unter `BbSpreadM15MaxX` Pips bleiben, damit der Preis auf dem Handelszeitrahmen zusammengepresst ist.
   * Der H4-ATR, umgerechnet in Pips, muss unter `AtrLimit` bleiben.

3. **Auslöser Eins – feste RSI-Levels**
   * RSI-Werte für M15, H1 und H4 müssen unter ihre jeweiligen "Low"-Schwellen fallen, um ein Long-Setup auszulösen, während sie über den "Low Limit"-Fail-Safes bleiben.
   * Der RSI muss gegenüber dem vorherigen M15-Wert um mehr als `RDeltaM15Lim1` steigen (Standard: –3.5 Pips) für Longs, oder um mehr als den gespiegelten Schwellenwert fallen für Shorts.
   * Die stochastische Hauptlinie muss für Longs unter `StocLoM15_1` oder für Shorts über `StocHiM15_1` liegen.
   * Short-Einstiege erfordern, dass RSI-Werte über ihren "High"-Schwellen liegen, aber unter den "High Limit"-Fail-Safes bleiben.

4. **Auslöser Zwei – adaptive RSI-Sigma-Bänder**
   * Historische RSI-Stichproben werden für jeden Zeitrahmen gehalten (bis zu `NumRsi` Elemente). Getrennte positive und negative Standardabweichungen werden berechnet, um asymmetrische Bollinger-ähnliche Bänder aufzubauen.
   * Dynamische untere und obere Bänder für jeden Zeitrahmen werden durch Anwenden von `Rsi*M*Sigma2`-Multiplikatoren (M15/H1/H4) erzeugt. Zusätzliche "Limit"-Multiplikatoren (`Rsi*M*SigmaLim2`) begrenzen die erlaubten Extreme.
   * Long-Einstiege erfordern, dass alle drei RSI-Werte unter ihren jeweiligen adaptiven unteren Bändern, aber über den unteren Limits liegen. Der Stochastik muss unter `StocLoM15_2` und die RSI-Steigung muss größer als `RDeltaM15Lim2` sein.
   * Short-Einstiege spiegeln die Logik mit oberen Bändern und Schwellen.

5. **Orderausführung und Ausstiege**
   * Wenn ein Auslöser auslöst, wird eine Market-Order der Größe `Volume` (Standard: 0.1 Lots) platziert.
   * Stop-Loss- und Take-Profit-Preise werden aus den konfigurierten Pip-Distanzen für den aktiven Auslöser (`StopLoss*X`, `TakeProfit*X`) mithilfe der Pip-Größen-Heuristik des Instruments abgeleitet (5-stellige und 3-stellige Symbole erhalten eine 10-fache Skalierung).
   * Schutzausstiege werden auf der nächsten M15-Kerze simuliert: Wenn das Hoch/Tief der Kerze den Stop oder das Take-Profit-Level berührt, schließt die Strategie die Position zum Markt und setzt die Schutzpreise zurück. Dies imitiert das MT5-Verhalten, das auf Stop-Orders basierte.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|--------------|----------|
| `Volume` | Handelsvolumen in Lots. | `0.1` |
| `TriggerOne` | Den festen RSI-Auslöser aktivieren. | `true` |
| `TriggerTwo` | Den adaptiven RSI-Auslöser aktivieren (gegenseitig exklusiv mit Auslöser eins). | `false` |
| `BbSpreadH4Min1` | Minimaler H4-Bollinger-Spread (Pips) für Auslöser eins. | `84` |
| `BbSpreadM15Max1` | Maximaler M15-Bollinger-Spread (Pips) für Auslöser eins. | `64` |
| `RsiPeriod1` | RSI-Länge verwendet von Auslöser eins auf allen Zeitrahmen. | `10` |
| `RsiLoM15_1`, `RsiHiM15_1` | RSI-Schwellen für M15. | `24`, `66` |
| `RsiLoH1_1`, `RsiHiH1_1` | RSI-Schwellen für H1. | `34`, `54` |
| `RsiLoH4_1`, `RsiHiH4_1` | RSI-Schwellen für H4. | `48`, `56` |
| `RsiLoLim*`, `RsiHiLim*` | Sicherheitslimits zum Blockieren extremer RSI-Werte. | `20–92` |
| `RDeltaM15Lim1` | Minimale RSI-Steigung auf M15 für Auslöser eins. | `-3.5` |
| `StocLoM15_1`, `StocHiM15_1` | Stochastische Grenzen für Auslöser eins. | `26`, `64` |
| `BbSpreadH4Min2` | Minimaler H4-Bollinger-Spread (Pips) für Auslöser zwei. | `65` |
| `BbSpreadM15Max2` | Maximaler M15-Bollinger-Spread (Pips) für Auslöser zwei. | `75` |
| `RsiPeriod2` | RSI-Länge verwendet von Auslöser zwei. | `20` |
| `NumRsi` | Stichprobengröße für RSI-Statistiken. | `60` |
| `Rsi*M*Sigma2` | Multiplikatoren für Haupt-adaptive Bänder (M15/H1/H4). | `1.20 / 0.95 / 0.9` |
| `Rsi*M*SigmaLim2` | Multiplikatoren für äußere Limits (M15/H1/H4). | `1.85 / 2.55 / 2.7` |
| `RDeltaM15Lim2` | Minimale RSI-Steigung auf M15 für Auslöser zwei. | `-5.5` |
| `StocLoM15_2`, `StocHiM15_2` | Stochastische Grenzen für Auslöser zwei. | `24`, `68` |
| `TakeProfitBuy1`, `StopLossBuy1` | Schutzabstände in Pips für Long-Auslöser eins. | `150`, `70` |
| `TakeProfitSell1`, `StopLossSell1` | Schutzabstände in Pips für Short-Auslöser eins. | `70`, `35` |
| `TakeProfitBuy2`, `StopLossBuy2` | Schutzabstände in Pips für Long-Auslöser zwei. | `140`, `35` |
| `TakeProfitSell2`, `StopLossSell2` | Schutzabstände in Pips für Short-Auslöser zwei. | `60`, `30` |
| `AtrPeriod` | H4-ATR-Berechnungsperiode. | `60` |
| `BollingerPeriod` | Bollinger-Bands-Länge auf M15 und H4. | `20` |
| `AtrLimit` | Maximaler ATR in Pips für erlaubte Einstiege. | `90` |
| `EntryHour` | Session-Startstunde (0–23). | `0` |
| `OpenHours` | Session-Länge in Stunden (0 = eine Stunde). | `14` |
| `NumPositions` | Maximale gleichzeitige Nettopositionen (Strategie öffnet nur bei flacher Position). | `1` |
| `FridayEndHour` | Freitags-Stunde, nach der der Handel stoppt. | `4` |
| `StochasticK`, `StochasticD`, `StochasticSlowing` | Parameter für den stochastischen Oszillator. | `12 / 5 / 5` |
| `M15CandleType`, `H1CandleType`, `H4CandleType` | Kerzendatentypen für jeden Zeitrahmen. | `15m / 1h / 4h` |

## Hinweise
* Die Schutz-Stop-Loss- und Take-Profit-Orders aus dem ursprünglichen EA werden durch Überwachung der M15-Kerzen-Hochs/Tiefs emuliert. Wenn Intrabar-Tick-Präzision erforderlich ist, sollten Stop-Orders über die Low-Level-API hinzugefügt werden.
* Stellen Sie sicher, dass das Portfolio Zugriff auf alle angeforderten Zeitrahmen bietet; andernfalls werden die Auslöser-Warteschlangen nicht gebildet und die Strategie bleibt inaktiv.
* Die Pip-Größen-Heuristik folgt der üblichen MetaTrader-Konvention: 5-stellige (oder 3-stellige für JPY-Kreuze) Symbole multiplizieren den Exchange-`PriceStep` mit 10.
