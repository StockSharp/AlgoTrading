# Crypto-Analysis-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein StockSharp-Port des MetaTrader-4-Expert-Advisors "Crypto Analysis". Sie sucht nach Ausbrüchen, die auftreten, nachdem der Preis das äußere Bollinger-Band im Haupt-Handelszeitrahmen berührt, während die Marktstruktur bärisch bleibt (schnelle LWMA unter der langsamen LWMA). Das System erlaubt Trades nur, wenn ein Momentum-Schub im höheren Zeitrahmen und ein monatlicher MACD-Filter mit der gewünschten Richtung übereinstimmen. Nach dem Einstieg wird die Position durch einen mehrschichtigen Schutzblock verwaltet, der den ursprünglichen EA nachbildet: pipbasierte Stops, geldbasierter Trail, Break-even-Verschiebung und Portfolio-Drawdown-Kontrollen.

## Handelslogik
- **Signalzeitrahmen:** konfigurierbar (standardmäßig M15). Alle Ein-/Ausstiegsregeln werden auf diesen Kerzen ausgewertet.
- **Volatilitätsauslöser:** Das Tief der vorherigen Kerze muss das untere Bollinger-Band (20, 2) berühren oder durchstoßen, um ein Long-Setup vorzubereiten; eine Berührung des oberen Bands bereitet ein Short-Setup vor.
- **Trendfilter:** Beide Szenarien verlangen, dass die schnelle linear gewichtete gleitende Durchschnittslinie (LWMA, Standard 6) unter der langsamen LWMA (Standard 85) bleibt, wodurch der bärische Bias des EA repliziert wird.
- **RSI-Bestätigung:** RSI(14) muss für Longs über 50 und für Shorts unter 50 liegen.
- **Momentum-Schub:** Die maximale absolute Abweichung der letzten drei Momentum(14)-Werte des höheren Zeitrahmens von der 100-Basislinie muss die Kauf-/Verkaufsschwellen überschreiten. Dies erfasst die Momentum-Spitzen des MQL-Codes.
- **Monatlicher MACD-Filter:** Ein separater monatlicher MACD (standardmäßig 30-Tage-Kerzen) (12, 26, 9) bestätigt die Richtung; Longs erfordern MACD-Hauptlinie über Signal, Shorts das Gegenteil.
- **Einstiegsausführung:** Sobald alle Filter übereinstimmen, eröffnet die Strategie eine Marktorder. Gegenpositionen werden vor einer Umkehr glattgestellt, um eine einzelne Nettoposition zu behalten, was dem Verhalten des EA beim Schließen entgegengesetzter Trades entspricht.

## Positionsverwaltung
- **Anfänglicher Stop und Ziel:** Konfigurierbare Distanzen in Pips werden aus der Tick-Größe des Instruments mit derselben 5-/3-stelligen Behandlung wie im EA umgerechnet (`0.00001`- und `0.001`-Schritte werden mit 10 multipliziert).
- **Trailing Stop:** Nach einem neuen Hoch (Long) oder Tief (Short) wird der Stop um `TrailingStopPips` hinter dem Preis nachgezogen, wobei immer das beste erreichte Niveau respektiert wird.
- **Break-even:** Wenn der Gewinn `BreakEvenTriggerPips` erreicht, wird der Stop auf den Einstiegspreis plus `BreakEvenOffsetPips` (Long) oder minus den Offset (Short) verschoben.
- **Geldziele:** Optionale absolute oder prozentbasierte Gewinnobergrenzen schließen die Position, sobald der schwebende PnL das angeforderte Niveau erreicht.
- **Geldbasierter Trail:** Sobald der nicht realisierte Gewinn `MoneyTrailTarget` überschreitet, verfolgt die Strategie den Spitzenwert und schließt die Position, wenn die Rückgabe `MoneyTrailStop` erreicht oder überschreitet.
- **Equity-Stop:** Die schwebende Equity (aktueller Portfoliowert plus nicht realisierter PnL) wird überwacht; wenn der Drawdown vom Höchstwert `EquityRiskPercent` überschreitet, wird die Position glattgestellt.

## Multi-Zeitrahmen-Daten
Drei Abonnements werden automatisch registriert:
1. Primäre Kerzenserie für Bollinger-/LWMA-/RSI-Regeln.
2. Kerzen des höheren Zeitrahmens für den Momentum-Filter (standardmäßig H1).
3. Monatskerzen für die MACD-Bestätigung (standardmäßig 30-Tage-Bars).

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `OrderVolume` | Basisordergröße. Gegenpositionen werden vor dem Öffnen einer neuen Position geschlossen. |
| `UseMoneyTakeProfit` | Aktiviert das absolute monetäre Take-Profit-Ziel. |
| `MoneyTakeProfit` | Gewinn in Portfoliowährung, der einen Ausstieg auslöst, wenn `UseMoneyTakeProfit` wahr ist. |
| `UsePercentTakeProfit` | Aktiviert das prozentbasierte Take-Profit-Ziel, berechnet aus der Anfangsequity. |
| `PercentTakeProfit` | Gewinnprozentsatz, der zum Schließen der Position erforderlich ist, wenn das Prozentziel aktiv ist. |
| `EnableMoneyTrailing` | Aktiviert den geldbasierten Trailing-Block. |
| `MoneyTrailTarget` | Gewinnniveau, ab dem der Geld-Trail startet. |
| `MoneyTrailStop` | Maximal erlaubte Gewinnrückgabe nach Erreichen von `MoneyTrailTarget`. |
| `StopLossPips` | Anfängliche Stop-Loss-Distanz in Pips. |
| `TakeProfitPips` | Anfängliche Take-Profit-Distanz in Pips. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. |
| `UseBreakEven` | Aktiviert die Break-even-Verschiebung des Stops. |
| `BreakEvenTriggerPips` | Pip-Gewinn, der vor Aktivierung des Break-even-Schutzes erforderlich ist. |
| `BreakEvenOffsetPips` | Zusätzliche Pips zum Einstiegspreis beim Setzen des Break-even-Stops. |
| `FastMaPeriod` | Länge der schnellen LWMA auf typischem Preis. |
| `SlowMaPeriod` | Länge der langsamen LWMA auf typischem Preis. |
| `MomentumPeriod` | Periode des Momentum-Indikators im höheren Zeitrahmen. |
| `MomentumBuyThreshold` | Minimale Momentum-Abweichung für Long-Einstiege. |
| `MomentumSellThreshold` | Minimale Momentum-Abweichung für Short-Einstiege. |
| `MacdFastLength` | Schnelle EMA-Länge für den MACD-Filter des höheren Zeitrahmens. |
| `MacdSlowLength` | Langsame EMA-Länge für den MACD-Filter des höheren Zeitrahmens. |
| `MacdSignalLength` | Signallänge für den MACD-Filter des höheren Zeitrahmens. |
| `UseEquityStop` | Aktiviert den Portfolio-Drawdown-Schutz. |
| `EquityRiskPercent` | Erlaubter Equity-Drawdown-Prozentsatz, bevor die Position zwangsweise geschlossen wird. |
| `CandleType` | Primärer Zeitrahmen für Einstiege. |
| `MomentumCandleType` | Höherer Zeitrahmen für die Momentum-Bestätigung. |
| `MacdCandleType` | Höherer Zeitrahmen für die MACD-Bestätigung. |

## Hinweise
- Der StockSharp-Port behält eine einzelne Nettoposition bei und entspricht damit dem EA, der Gegenorders vor einem neuen Trade schließt.
- Alle Schutzregeln arbeiten auf geschlossenen Kerzen, um die "neue Kerze"-Verarbeitung des ursprünglichen Skripts nachzubilden.
- Bei synthetischen Symbolen oder Instrumenten ohne Standard-Pip-Größe passen Sie `StopLossPips` und verwandte Parameter an den Tick-Wert der Börse an.
