# SMC Hilo MaxMin Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reproduziert das Verhalten des MetaTrader-Experten *SMC MaxMin bei 1200*. Zur angegebenen Endzeit platziert es a
Eine Buy-Stop-Order über dem Hoch der vorherigen Kerze und eine Sell-Stop-Order unter dem Tief der vorherigen Kerze. Ausstehende Bestellungen werden aufgefüllt
durch den Mindeststoppabstand des Brokers, umgerechnet von Pips in Instrumentenpreiseinheiten. Bei einem Ausbruch erfolgt die umgekehrte Reihenfolge
wird aufgehoben und die offene Position wird durch feste Stops, Gewinnziele und einen optionalen Trailing Stop verwaltet.

Hauptunterschiede zum ursprünglichen MQL4-Code:

- StockSharp-Bestellgrundelemente (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) ersetzen die direkten `OrderSend`-Aufrufe.
- Die Mindest-Stopp-Distanz, Stop-Loss- und Take-Profit-Eingaben werden in Pips ausgedrückt und über `Security.PriceStep` in umgerechnet
Beachten Sie die tatsächliche Tick-Größe des Instruments.
- Das Trailing-Stop-Management verschiebt die Stop-Order nur, wenn eine profitable Distanz erreicht wird, die größer als der Trailing-Puffer ist.
- Die gesamte Logik wird durch das High-Level-Kerzenabonnement API gesteuert, sodass keine direkten Verlaufsscans oder manuellen Indikatorpuffer verwendet werden.

## Handelsregeln
1. **Einrichtungsstunde** – wenn die Endstunde `SetHour` beträgt, verwenden Sie die zuvor abgeschlossene Kerze als Referenz.
2. **Langer Einstieg** – Setzen Sie einen Kaufstopp bei `previous_high + min_stop_distance + price_step`.
3. **Short-Einstieg** – setzen Sie einen Verkaufsstopp bei `previous_low - min_stop_distance - price_step`.
4. **Gegenseitige Ausschließlichkeit** – wenn einer der Stopps erfüllt ist, wird die entgegengesetzte ausstehende Order sofort storniert.
5. **Stop-Loss** – der Long-Stop ist `previous_low - StopLossPips`, der Short-Stop ist `previous_high + StopLossPips` (beide umgerechnet).
zu Preiseinheiten).
6. **Take-Profit** – Long-Positionen verwenden ein Verkaufslimit bei `entry + TakeProfitPips`; Short-Positionen nutzen ein Kauflimit bei
`entry - TakeProfitPips`.
7. **Trailing Stop** – wenn eine Position einen Gewinn von mehr als `TrailingStopPips` hat, wird der Stop nachgezogen, um den gleichen Pip beizubehalten
Abstand vom aktuellen Geld-/Briefkurs.
8. **Auftragszeitlimit** – zwei Stunden nach der Einrichtung (`SetHour + 2`) werden alle nicht gefüllten ausstehenden Stopps storniert.

## Parameter
| Name | Beschreibung | Standard |
| ---- | ----------- | ------- |
| `Volume` | Für beide Einstiegsaufträge verwendetes Auftragsvolumen. | `0.1` |
| `SetHour` | Endstunde (0–23), zu der der Breakout-Straddle erstellt wird. | `15` |
| `TakeProfitPips` | Gewinnzielentfernung in Pips. Auf `0` setzen, um Take-Profit-Bestellungen zu deaktivieren. | `500` |
| `StopLossPips` | Schutzstoppabstand in Pips. Auf `0` setzen, um den anfänglichen Stopp zu deaktivieren. | `30` |
| `TrailingStopPips` | Distanz für den Trailing Stop in Pips. Auf `0` setzen, um einen statischen Stopp beizubehalten. | `30` |
| `MinStopDistancePips` | Mindeststoppabstand des Brokers, der zur Aufstockung der Einstiegspreise verwendet wird. | `0` |
| `CandleType` | Der Kerzentyp, der die stündliche Sitzung definiert, ist standardmäßig auf einen Zeitrahmen von 1 Stunde eingestellt. | `1h` |

## Nutzungshinweise
- Die Strategie erfordert Level-1-Daten, um Trailing-Stops zu verwalten und die neuesten Geld-/Briefkurse für Distanzberechnungen beizubehalten.
- Wenn das zugrunde liegende Instrument nicht standardmäßige Tick-Größen aufweist (z. B. JPY-Kreuzungen mit 0,01 Pip), passen Sie `TakeProfitPips` an.
`StopLossPips` und `TrailingStopPips` entsprechend.
- Wenn `TakeProfitPips` oder `StopLossPips` Null ist, werden die entsprechenden Aufträge nicht übermittelt, aber Trailing Stops können dennoch aktiviert werden, wenn
Der nachgestellte Parameter ist positiv.
- Stellen Sie sicher, dass der konfigurierte `SetHour` mit der Broker-Serverzeit des eingehenden Datenfeeds übereinstimmt.
