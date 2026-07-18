# Twenty-Pips-Preiskanalstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Twenty-Pips-Preiskanalstrategie ist eine Umsetzung des ursprünglichen MetaTrader-Expertenberaters *20 Pips*, die einen Preiskanal im Donchian-Stil mit kurzfristigen gleitenden Durchschnittsfiltern kombiniert. Der Algorithmus öffnet Trades nur, wenn die aktuelle Kerze entgegengesetzt zur vorherigen öffnet, filtert die Richtung mit gleitenden Durchschnitten, die auf typischen Preisen berechnet werden, und verwaltet Ausstiege über ein festes Zwanzig-Pip-Ziel, das durch einen dynamischen kanalbasierten Trailing Stop unterstützt wird.

Die StockSharp-Version behält den Geist des ursprünglichen Ansatzes bei und passt die Auftragsverwaltung gleichzeitig an die übergeordnete API-Version an. Marktaufträge werden für Ein- und Ausstiege verwendet, Gewinnziele werden intern überwacht und Stop-Levels werden mit Preiskanalbedingungen nachgebildet.

## Handelslogik

1. **Indikatorstapel**
   - Ein einfacher gleitender Durchschnitt einer Periode des typischen Preises (H+L+C)/3 dient als schnelle Basislinie, die den typischen Preis der vorherigen Kerze widerspiegelt.
   - Ein konfigurierbarer langsamer einfacher gleitender Durchschnitt (Standard 20), der auf Schlusskursen berechnet wird, spielt die Rolle des `MA_Low`-Filters aus dem EA.
   - Höchste und niedrigste Indikatoren mit demselben Zeitraum wie der Preiskanal (Standard 20) emulieren die ursprünglichen benutzerdefinierten Indikatorpuffer.

2. **Eintrittsbedingungen**
   - Long-Setup: Der vorherige schnelle typische Preis liegt über dem vorherigen langsamen gleitenden Durchschnitt **und** die aktuelle Kerze öffnet sich unter der vorherigen Eröffnung. Nach einem Verlustgeschäft wird das Volumen mit dem Erholungsfaktor multipliziert (Standard 2). Der Einstiegspreis wird erfasst, um Gewinn und Verlust zu verfolgen.
   - Short-Setup: Der vorherige schnelle typische Preis liegt unter dem vorherigen langsamen gleitenden Durchschnitt **und** die aktuelle Kerze öffnet sich über der vorherigen Eröffnung. Die Volumenskalierung folgt der gleichen Erholungslogik wie bei Long-Trades.

3. **Exit-Management**
   - Bei der Eröffnung der Position wird ein festes Take-Profit-Ziel in Höhe von `TakeProfitPips` multipliziert mit der Preisstufe des Instruments festgelegt.
   - Ein kanalgesteuerter Trailing Stop ahmt den ursprünglichen `OrderModify`-Aufruf nach. Wenn der vorherige Balken den Preiskanal durchbricht (Verschiebung um zwei Balken aus der MT4-Logik), wird der Schutzstopp zum vorherigen Extrem minus/plus dem nachlaufenden Offset in Pips verschoben. Wenn die nächste Kerze über dieses Extrem hinausgeht, wird die Position sofort zum Eröffnungspreis geschlossen.
   - Take-Profit-, Trailing-Stop- und Gap-Exits werden alle über Marktaufträge ausgeführt, während der tatsächliche Exit-Preis verfolgt wird, um die Gewinn-/Verlust-Flagge für die Martingal-Skalierung zu aktualisieren.

4. **Martingale Wiederherstellung**
   - Nach jeder geschlossenen Verlustposition wird die nächste Einstiegsgröße mit `RecoveryMultiplier` multipliziert. Bei gewinnbringenden Geschäften wird die Flagge zurückgesetzt und auf das Basisvolumen zurückgesetzt.

## Parameter

| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Primärer Zeitrahmen, der für Berechnungen verwendet wird. | 1 Stunde Kerzen |
| `ChannelPeriod` | Lookback-Zeitraum für den Kanal im Donchian-Stil. | 20 |
| `SlowMaPeriod` | Länge des langsam gleitenden Durchschnittsfilters. | 20 |
| `TakeProfitPips` | Abstand in Pips für das festgelegte Gewinnziel. | 20 |
| `TrailingOffsetPips` | Offset, der beim Anziehen des Anschlags auf das vorherige Extrem verwendet wird. | 10 |
| `RecoveryMultiplier` | Nach einem Verlust angewendeter Volumenmultiplikator. | 2 |
| `Volume` | Basishandelsvolumen vor Erholungsskalierung. | 0,1 |

## Nutzungshinweise

- Die Strategie geht davon aus, dass `Security.PriceStep` den Pip-Wert des gehandelten Instruments widerspiegelt. Passen Sie `TakeProfitPips` und `TrailingOffsetPips` an, wenn das Symbol eine andere Pip-Definition verwendet.
- Da StockSharp Marktaufträge für Ausstiege verwendet, können Backtests im Vergleich zu den ursprünglichen MT4-Stop- und Limit-Aufträgen einen Slippage aufweisen. Die Logik reproduziert immer noch die gleichen Preisschwellen.
- Die Kanalwerte werden verschoben, um die `iCustom(..., shift=2)`-Aufrufe zu emulieren; Beachten Sie dies, wenn Sie das Nachlaufverhalten ändern.
- Der Wiederherstellungsmultiplikator kann auf 1 gesetzt werden, um die Martingal-Skalierung zu deaktivieren.
