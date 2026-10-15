# MA Price Cross-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die MA Price Cross-Strategie ist eine direkte Umwandlung des MetaTrader 4-Expertenberaters „MA Price Cross“ in den StockSharp High-Level-API. Es wartet darauf, dass der ausgewählte gleitende Durchschnitt den aktuellen Preis kreuzt, während der Handel innerhalb eines konfigurierbaren Zeitfensters zulässig ist. Wenn die Kreuzung von unten erfolgt, eröffnet der Algorithmus eine Long-Position; Wenn die Kreuzung von oben erfolgt, wird eine Short-Position eröffnet. Schützende Stop-Loss- und Take-Profit-Abstände werden in MetaTrader Punkten definiert und mithilfe des `PriceStep` des Instruments automatisch in absolute Preisversätze umgerechnet.

Im Gegensatz zur ursprünglichen MQL-Implementierung, die auf jeden Tick reagiert, arbeitet die StockSharp-Version mit fertigen Kerzen und verwendet das High-Level-Abonnement `SubscribeCandles`. Dadurch wird sichergestellt, dass Handelsentscheidungen einmal pro Balken ausgeführt werden und mit der Indikatorbindungspipeline kompatibel bleiben. Der gleitende Durchschnitt kann so konfiguriert werden, dass er allen vier MetaTrader-Modi entspricht, und akzeptiert verschiedene Preisquellen (Schlusskurs, Eröffnungstag, Höchstkurs, Tiefstkurs, Medianwert, typisch, gewichtet).

## Handelslogik

1. Warten Sie, bis die aktuelle Zeit in das Handelsfenster `[StartTime, StopTime)` fällt. Nachtfenster werden dadurch unterstützt, dass sie um Mitternacht gewickelt werden.
2. Verarbeiten Sie nur fertige Kerzen. Füttere den konfigurierten gleitenden Durchschnitt mit dem gewählten angewandten Preis.
3. Speichern Sie den vorherigen gleitenden Durchschnittswert, um die in MetaTrader verwendete `iMA`-Verschiebungslogik zu emulieren.
4. Wenn der vorherige Durchschnitt unter dem letzten Preis und der neue Durchschnitt über dem Preis liegt, eröffnen Sie eine Long-Position (oder kehren Sie diese um).
5. Wenn der vorherige Durchschnitt über dem letzten Preis liegt und der neue Durchschnitt unter dem Preis, eröffnen Sie eine Short-Position (oder gehen Sie eine Short-Position ein).
6. Bevor Sie eine neue Position auf der gegenüberliegenden Seite öffnen, reduzieren Sie alle vorhandenen Belichtungen, um die `OrdersTotal() == 0`-Beschränkung des ursprünglichen Codes widerzuspiegeln.
7. Starten Sie einen virtuellen Stop-Loss und Take-Profit mit Distanzen ausgedrückt in MetaTrader Punkten multipliziert mit dem aktuellen Instrument `PriceStep`.

## Standardparameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | `TimeFrame(1m)` | Kerzenserie, die alle Berechnungen vorantreibt. |
| `MaPeriod` | `160` | Anzahl der vom gleitenden Durchschnitt verwendeten Balken. |
| `MaMethod` | `Simple` | Typ des gleitenden Durchschnitts: Einfach, Exponentiell, Geglättet oder LinearGewichtet. |
| `PriceType` | `Close` | Preisquelle, die an den gleitenden Durchschnitt weitergeleitet wird (Schluss/Eröffnung/Hoch/Tief/Median/typisch/gewichtet). |
| `StartTime` | `01:00` | Tageszeit, zu der der Handel aktiv wird. |
| `StopTime` | `22:00` | Tageszeit, zu der neue Einträge enden. |
| `StopLossPoints` | `200` | MetaTrader Punkte umgerechnet in einen absoluten Schutzstoppabstand. |
| `TakeProfitPoints` | `600` | MetaTrader Punkte werden in eine absolute Gewinnzieldistanz umgewandelt. |
| `OrderVolume` | `0.1` | Mit Marktaufträgen übermitteltes Standardvolumen. |

## Notizen

- Wenn `StartTime` gleich `StopTime` ist, ist der Zeitfilter deaktiviert und der Handel ist den ganzen Tag über zulässig.
- Wenn `StopLossPoints` oder `TakeProfitPoints` gleich Null ist, wird die entsprechende Schutzstufe nicht registriert.
- Der Zeitfilter verwendet die Kerzenschlusszeit (`candle.CloseTime.TimeOfDay`), sodass er sich an die von MarketDataConnector bereitgestellte Börsenzeitzone anpasst.
- Wenn die Sicherheit `PriceStep` nicht verfügbar macht, werden punktbasierte Entfernungen direkt ohne Skalierung verwendet.

## Ursprüngliche Strategiereferenz

- Quelle: `MQL/44133/MA Price Cross.mq4`
- Autor: JBlanked (2023)
