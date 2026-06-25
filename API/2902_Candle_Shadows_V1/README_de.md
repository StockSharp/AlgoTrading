# Kerzenschatten V1-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Kerzenschatten V1 ist eine Preisaktions-Umkehrstrategie, die die ursprüngliche MetaTrader-Expertenberater-Logik in der High-Level-API von StockSharp nachbildet. Das System sucht nach Kerzen mit einem starken dominanten Docht und minimalem gegenüberliegenden Schatten während einer konfigurierbaren Handelssitzung. Trades sind nur während der ersten Minuten einer Barre erlaubt, was die intrabar-Ausführung der MQL-Version emuliert, während sie weiterhin auf geschlossenen Kerzen arbeitet.

## Handelslogik
1. Die konfigurierten Zeitrahmen-Kerzen abonnieren (Standard 5 Minuten) und nur abgeschlossene Balken auswerten.
2. Ein Sitzungsfenster mit den Parametern `StartHour` und `EndHour` durchsetzen. Wenn die Kerze außerhalb des Fensters öffnet, wird kein Trade in Betracht gezogen.
3. Einstiege nur erlauben, wenn die Kerze vor `OpenWithinMinutes` von ihrer Öffnungszeit schließt, um späte Signale auf langen Balken zu verhindern.
4. Long-Setup: die Kerze muss einen unteren Schatten größer als `CandleSizeMinPips` Pips drucken und der obere Schatten muss innerhalb von `OppositeShadowMaxPips` Pips bleiben. Wenn die Bedingungen erfüllt sind und keine offene Position vorhanden ist, wird ein Markt-Kauf gesendet.
5. Short-Setup: die Kerze muss einen oberen Schatten größer als `CandleSizeMinPips` Pips drucken und der untere Schatten muss innerhalb von `OppositeShadowMaxPips` Pips bleiben. Ein Markt-Verkauf wird ausgegeben, wenn das Konto flach ist.
6. Nur ein Trade pro Kerze ist erlaubt, was der ursprünglichen Einschränkung "eine Order pro Balken" entspricht.

## Positionsverwaltung
- Anfängliche Schutzabstände werden in Pips ausgedrückt und über den `PipValue`-Parameter für jedes Instrument umgerechnet.
- Harte Stop-Loss- und Take-Profit-Prüfungen werden bei jeder abgeschlossenen Kerze durchgeführt. Wenn das Hoch/Tief der Kerze den Schwellenwert berührt, wird die Position geflacht.
- Das Trailing-Management ahmt den MQL-Trailing Stop nach: sobald der Preis um mindestens `TrailingStopPips + TrailingStepPips` voranschreitet, wird der Stop in Inkrementen von `TrailingStepPips` Pips bewegt.
- Wenn eine Position länger als `PositionLivesBars` Balken offen bleibt, wird sie sofort geschlossen. Profitable Trades werden auch nach `CloseProfitsOnBar` Balken herausgedrückt, um Gewinne zu sichern.
- Das nächste Trade-Volumen wird reduziert, indem `BaseVolume` durch `LossReductionFactor` geteilt wird, wenn der vorherige Trade mit einem Verlust geschlossen wurde, genau wie die Lot-Reduzierung im ursprünglichen Expertenberater.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `PipValue` | Monetärer Wert eines Pips, der zur Umwandlung von Pip-Distanzen in Preisversätze verwendet wird. | `0.0001` |
| `StopLossPips` | Stop-Loss-Distanz in Pips. Auf `0` setzen, um den harten Stop zu deaktivieren. | `50` |
| `TakeProfitPips` | Take-Profit-Distanz in Pips. Auf `0` setzen, um das harte Ziel zu deaktivieren. | `50` |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. Wenn `0`, wird kein Trailing angewendet. | `15` |
| `TrailingStepPips` | Mindestschritt in Pips zwischen Trailing-Stop-Anpassungen. Muss positiv sein, wenn Trailing aktiviert ist. | `5` |
| `PositionLivesBars` | Maximale Anzahl abgeschlossener Balken, die eine Position offen bleiben kann, bevor sie zwangsgeschlossen wird. | `4` |
| `CloseProfitsOnBar` | Wenn größer als null, werden profitable Positionen nach dieser Anzahl von Balken ab dem Einstieg geschlossen. | `2` |
| `OpenWithinMinutes` | Maximale Anzahl von Minuten nach Balken-Öffnung, wenn neue Trades erlaubt sind. | `7` |
| `CandleSizeMinPips` | Erforderliche Docht-Länge (in Pips) auf der dominanten Seite der Kerze. | `15` |
| `OppositeShadowMaxPips` | Maximale Größe (in Pips) des gegenüberliegenden Kerzenschattens. | `1` |
| `StartHour` | Sitzungsstartzeit in Börsenzeit (0–23). | `6` |
| `EndHour` | Sitzungsendzeit in Börsenzeit (0–23). | `18` |
| `LossReductionFactor` | Divisor, der auf `BaseVolume` nach einem verlierenden Trade angewendet wird. | `1.5` |
| `BaseVolume` | Standardmäßige Marktauftragsgröße für Einstiege. | `1` |
| `CandleType` | Kerzenserie für die Berechnungen. Standard ist ein 5-Minuten-Zeitrahmen. | `5 min` |

## Hinweise
- `PipValue` immer anpassen, um der Tick-Größe des Instruments zu entsprechen (zum Beispiel `0.01` für JPY-Kreuze oder `1` für Index-Futures).
- Da die Strategie mit abgeschlossenen Kerzen arbeitet, erfolgen Ausführungen beim Balken-Schluss. Niedrigere Zeitrahmen (1–5 Minuten) replizieren am besten das intrabar-Verhalten des ursprünglichen Expertenberaters.
- Keine externen Indikatoren erforderlich, was die Strategie einfach auf jeder StockSharp-Datenquelle ausführbar macht.
