# Nirvaman Imax-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Nirvaman Imax-Strategie ist eine direkte Umsetzung des MetaTrader 4 Expert Advisors `NirvamanImax.mq4` gebündelt mit den benutzerdefinierten Indikatoren HA, Moving Averages2 und iMAX3alert. Die StockSharp-Implementierung behält die ursprüngliche Idee der Kombination von Heikin-Ashi-Kerzen mit einem zweiphasigen Trenddetektor und einem EMA-Basislinienfilter bei und übernimmt gleichzeitig das High-Level-API. Die Strategie funktioniert auf einem einzigen Instrument und Zeitrahmen und schließt Geschäfte automatisch nach einer konfigurierbaren Haltedauer.

## Indikatoren und Filter
- **Heikin-Ashi Kerzen** – Reproduzieren Sie den ursprünglichen HA-Indikator und klassifizieren Sie Kerzen als bullisch oder bärisch, indem Sie die Heikin-Eröffnungs- und Schlusswerte vergleichen.
- **Schnelle/langsame EMA-Frequenzweiche** – ersetzt den MT4 `iMAX3alert1`-Zweiphasenausgang. Ein bullisches Signal erscheint, wenn der schnelle EMA den langsamen EMA überschreitet; Beim gegenüberliegenden Crossover tritt ein rückläufiges Signal auf.
- **EMA-Trendfilter** – spiegelt den `Moving Averages2` EMA-Puffer wider und fungiert als Basislinie. Es sind nur Long-Trades oberhalb des Filters und Short-Trades darunter zulässig.
- **Zeitfilter** – überspringt jede Kerze, deren Stunde innerhalb des verbotenen Fensters liegt, das durch `NoTradeStartHour` und `NoTradeEndHour` definiert ist (das Fenster unterstützt um Mitternacht und eine Broker-Zeitzonenverschiebung).
- **Zeitgesteuerter Ausstieg** – jede Position wird nach Ablauf von `CloseAfter` zwangsweise geschlossen, wodurch die `tiempoCierre`-Logik der MQL-Version reproduziert wird.
- **Stops und Ziele** – Stop-Loss und Take-Profit werden in Preisschritten angewendet, die von der Tick-Größe des Instruments abgeleitet werden. Wenn Sie entweder `0` festlegen, wird der entsprechende Schutz deaktiviert.

## Handelsregeln
1. Warten Sie, bis Heikin-Ashi, schneller EMA, langsamer EMA und Filter EMA gebildet sind und ein früherer Kerzenschluss verfügbar ist.
2. Lehnen Sie das Signal ab, wenn die Kerzenzeit innerhalb des eingeschränkten Handelsfensters liegt.
3. Langer Eintrag:
   - Der schnelle EMA kreuzt den langsamen EMA der aktuellen Kerze.
   - Der Heikin-Ashi-Schlusskurs liegt über seinem Eröffnungskurs (bullischer Körper).
   - Der vorherige Kerzenschluss liegt über dem EMA-Filter.
4. Kurzer Eintrag:
   - Der schnelle EMA kreuzt den langsamen EMA der aktuellen Kerze.
   - Der Schlusskurs von Heikin-Ashi liegt unter seinem Eröffnungskurs (bärischer Körper).
   - Der vorherige Kerzenschluss liegt unter dem EMA-Filter.
5. Ausgangsregeln:
   - Stop-Loss- oder Take-Profit-Level werden durch die Kerzenspanne berührt.
   - Die maximale Positionslebensdauer `CloseAfter` ist überschritten.
   - Der über `StartProtection()` ausgelöste manuelle Schutz schließt die Position, wenn die Engine dies anfordert.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `TradeVolume` | Basis-Market-Order-Volumen. | `0.1` |
| `CandleType` | Kerzenzeitrahmen, der für jeden Indikator und jedes Signal verwendet wird. | `30m` Zeitrahmen |
| `FastTrendLength` | Länge des schnellen EMA, der die blaue iMAX-Phase emuliert. | `10` |
| `SlowTrendLength` | Länge des langsamen EMA, der die rote iMAX-Phase emuliert. | `21` |
| `FilterLength` | EMA Zeitraum für den Basisfilter (Äquivalent zu gleitenden Durchschnitten2). | `13` |
| `StopLoss` | Schutzanschlagabstand in Preisschritten; `0` deaktiviert den Stopp. | `50` |
| `TakeProfit` | Gewinnzielentfernung in Preisschritten; `0` deaktiviert das Ziel. | `100` |
| `CloseAfter` | Maximale Haltezeit, bevor die Position zwangsweise geschlossen wird. | `15000 s` |
| `NoTradeStartHour` | Stunde (0–23), die den Beginn des No-Trade-Fensters markiert. | `22` |
| `NoTradeEndHour` | Stunde (0–23), die das Ende des No-Trade-Fensters markiert. | `2` |
| `BrokerTimeOffset` | Der Zeitzonenversatz des Brokers (Stunden), der vor dem Zeitfilter angewendet wird. | `0` |

## Konvertierungshinweise
- Der MT4-Indikator `iMAX3alert1` stellt zwei farbcodierte Puffer bereit. Ihr Crossover wird in einen Fast/Slow-EMA-Crossover übersetzt, der die ursprüngliche ereignisgesteuerte Eintrittslogik beibehält.
- Der Moving Averages2-Indikator wurde im Modus EMA mit einer Standardlänge von 13 ausgeführt. Die Version StockSharp verwendet einen Standard-`ExponentialMovingAverage` mit demselben Standardwert wieder.
- Die Verwaltung des Positionslebenszyklus spiegelt das Skript MQL wider: Die Position wird nach einer Zeitüberschreitung geschlossen, bevor neue Einträge ausgewertet werden können, und es wurde keine zusätzliche Trailing-Stop-Logik hinzugefügt.

## Anwendungstipps
1. Befestigen Sie die Strategie an einem Board/Wertpapier und stellen Sie den gewünschten `CandleType` ein, bevor Sie damit beginnen.
2. Passen Sie `TradeVolume`, `StopLoss`, `TakeProfit` und `CloseAfter` an die Volatilität und Risikotoleranz des Instruments an.
3. Optimieren Sie die EMA-Zeiträume, wenn Sie das Verhalten des ursprünglichen iMAX-Tunings für einen neuen Markt annähern müssen.
4. Kombinieren Sie es mit Risikokontrollen auf höherer Ebene (Portfolioschutz, Sitzungskontrolle), wenn Sie mehrere Instanzen ausführen.
