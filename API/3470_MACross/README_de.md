# MACross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie repliziert das Verhalten des ursprünglichen `MQL/34176/MACross.mq4`-Expertenberaters unter Verwendung des StockSharp-High-Level-API. Es handelt ein einzelnes Instrument mit einem Crossover mit gleitendem Durchschnitt und behält alle Risikokontrollen bei, ausgedrückt in Pips und Kontokapital.

## Handelslogik

1. Zwei einfache gleitende Durchschnitte (SMA) basieren auf dem konfigurierten Kerzentyp:
   - `FastPeriod` reagiert schnell auf Preisänderungen.
   - `SlowPeriod` glättet den längerfristigen Trend.
2. Am Ende jeder fertigen Kerze werden die schnellen und langsamen Durchschnittswerte verglichen:
   - Ein bullischer Crossover (schneller Übergang über langsam) eröffnet eine Long-Position. Jeder aktive Short wird zuerst abgeflacht.
   - Ein bärischer Crossover (schneller Übergang unter langsam) eröffnet eine Short-Position, nachdem eine bestehende Long-Position geschlossen wurde.
3. Jeder Eintrag verwendet ein festes Marktvolumen, das von `LotSize` abgeleitet und an den Instrumentenlimits (`VolumeStep`, `MinVolume`, `MaxVolume`) ausgerichtet ist.
4. Nachdem die Position eröffnet wurde, verfolgt die Strategie zwei in Pips gemessene Risikoziele. Die Pip-Größe wird automatisch aus `Security.Decimals` (oder `PriceStep` als Fallback) abgeleitet:
   - `TakeProfitPips` definiert den Abstand zum Gewinnziel. Ein Treffer führt zu einem Marktausstieg in die aktuelle Richtung.
   - `StopLossPips` definiert den Schutzstoppabstand. Bei einem Verstoß wird die Position sofort geschlossen.
5. Der Handel kann vom `MinEquity`-Wächter unterbrochen werden. Wenn der aktuelle Portfoliowert unter dem Schwellenwert liegt, verwaltet die Strategie weiterhin die aktive Position, lässt jedoch keine neuen Einträge zu.

Alle Berechnungen funktionieren nur bei fertigen Kerzen und stimmen vollständig mit dem ursprünglichen Expertenberater überein, der auf einen neuen Balken wartete, bevor er die gleitenden Durchschnitte auswertete.

## Visualisierung

Wenn ein Diagrammbereich verfügbar ist, werden die Strategiediagramme wie folgt dargestellt:

- Geben Sie Kerzen aus der abonnierten Serie ein.
- Die schnellen und langsamen SMAs.
- Eigene Trades zur Hervorhebung von Ein- und Ausstiegen, die durch die Crossover-Regeln ausgelöst werden.

## Parameter

| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `8` | Länge des schnellen SMA, der Crossover-Signale erzeugt. |
| `SlowPeriod` | `int` | `20` | Länge des langsamen SMA, der als Referenztrendlinie verwendet wird. |
| `TakeProfitPips` | `decimal` | `20` | Gewinnzieldistanz ausgedrückt in Pips. Die Pip-Größe wird aus den Dezimalstellen des Instruments abgeleitet. |
| `StopLossPips` | `decimal` | `20` | Schutzstoppabstand in Pips. Verwendet die gleiche Pip-Größenberechnung wie das Gewinnziel. |
| `LotSize` | `decimal` | `1` | Grundauftragsvolumen. Die Strategie rundet den Wert auf die nächste zulässige Größe, bevor Marktaufträge gesendet werden. |
| `MinEquity` | `decimal` | `100` | Mindestkontoguthaben. Solange der Portfoliowert unter diesem Niveau liegt, werden neue Geschäfte blockiert. |
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Kerzenreihe, die für SMA-Berechnungen und Signalauswertung verwendet wird. |

## Unterschiede zur MQL-Version

- Der ursprüngliche MQL-Experte hat Stop-Loss- und Take-Profit-Preise für `OrderSend` als Null angenommen. Der StockSharp-Port emuliert das gleiche Verhalten mit manuellen Exits, die den Schlusskurs jeder fertigen Kerze überwachen.
- Die Eigenkapitalvalidierung (`cekMinEquity`) liest jetzt `Portfolio.CurrentValue` und `Portfolio.BeginValue` statt `AccountEquity()`, behält aber die Schwellenwertlogik bei.
- Die Pip-Größenerkennung spiegelt den `GetPipPoint`-Helfer wider: 2- oder 3-stellige Anführungszeichen verwenden 0,01, 4- oder 5-stellige Anführungszeichen verwenden 0,0001, andernfalls wird `PriceStep` verwendet.

Die resultierende Strategie kann über alle exponierten Parameter optimiert werden und lässt sich nahtlos mit der Diagramm- und Risikomanagement-Infrastruktur von StockSharp kombinieren.
