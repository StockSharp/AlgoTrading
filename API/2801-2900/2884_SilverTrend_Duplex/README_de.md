# SilverTrend Duplex-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht

Die **SilverTrend Duplex-Strategie** ist ein StockSharp-Port des MetaTrader 5-Expertenberaters `Exp_SilverTrend_Duplex`. Der ursprüngliche Roboter kombiniert zwei unabhängige SilverTrend-Filter (für Long- und Short-Entscheidungen) und führt Trades aus, wenn die Indikatorfarben zwischen bullischen und bärischen Zuständen wechseln. Diese C#-Implementierung behält die Doppelfilter-Architektur bei, sodass Sie die Long- und Short-Logik separat abstimmen können, während Sie die StockSharp High-Level-API nutzen.

Die Strategie arbeitet nur auf abgeschlossenen Kerzen. Zwei separate Abonnements können konfiguriert werden, sodass Long- und Short-Signale bei Bedarf verschiedene Zeitrahmen oder Instrumente beobachten können. Intern rekonstruiert ein benutzerdefinierter `SilverTrendIndicator` die Farblogik aus der MQL-Version durch Kombination von Donchian-Kanal-Extremwerten mit dem Risiko-Multiplikator zur Emulation der ursprünglichen SilverTrend-Bänder.

## Handelslogik

1. **Indikator-Rekonstruktion**
   - Für jede Kerze werden die obere und untere Donchian-Grenze über `SSP` Bars berechnet.
   - Adaptive Schwellenwerte `smin` und `smax` werden mit dem Risikokoeffizienten (`33 - risk`) abgeleitet, identisch zum MQL-Algorithmus.
   - Wenn der Preis über `smax` schließt, wird ein bullischer Zustand aufgezeichnet; schließt er unter `smin`, wird ein bärischer Zustand aufgezeichnet; andernfalls wird der vorherige Zustand beibehalten. Die Richtung des Kerzenkörpers bestimmt den endgültigen Farbcode (0..4) genau wie im ursprünglichen SilverTrend-Indikator.

2. **Signalvorbereitung**
   - Farbwerte werden für die jüngsten `SignalBar + 1` abgeschlossenen Kerzen sowohl für Long- als auch Short-Filter gespeichert.
   - Long-Signale lösen aus, wenn die Farbe am gewählten Offset unter `2` fällt (bullisch), während die vorherige Farbe größer als `1` war (nicht bullisch), und repliziert `Value[1] < 2 && Value[0] > 1` aus MQL.
   - Short-Signale lösen aus, wenn die Farbe über `2` steigt (bärisch) und die vorherige Farbe über `0` liegt, übereinstimmend mit `Value[1] > 2 && Value[0] > 0` aus dem Skript.

3. **Orderausführung**
   - Einstiege verwenden `BuyMarket` oder `SellMarket` mit einem Volumen gleich `Volume + |Position|`, das sowohl jede entgegengesetzte Exposition schließt als auch die neue Seite in einer einzigen Marktorder eröffnet.
   - Ausstiege beruhen darauf, dass der Indikator zur entgegengesetzten Farbband zurückkehrt. Long-Positionen werden geschlossen, wenn die Farbe über `2` steigt, Short-Positionen wenn sie unter `2` fällt.

Die Strategie repliziert nicht die ursprüngliche Geldmanagement-Matrix oder serverseitige Stop-Platzierung aus `TradeAlgorithms.mqh`. Risikosteuerung sollte daher über die integrierten Schutzmechanismen von StockSharp oder Broker-Regeln verwaltet werden.

## Parameter

| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `LongCandleType` | 4-Stunden-Kerzen | Datentyp für den Long-seitigen Indikator. |
| `LongSsp` | 9 | SilverTrend-Lookback-Länge für den Long-Filter. |
| `LongRisk` | 3 | Risiko-Multiplikator (`33 - risk`) auf die Kanalbreite angewendet. |
| `LongSignalBar` | 1 | Offset (in abgeschlossenen Kerzen) für die Auswertung von Long-Signalen. Muss ≥ 1 sein. |
| `EnableLongEntries` | true | Schaltet das Öffnen von Long-Positionen um. |
| `EnableLongExits` | true | Schaltet das Schließen von Long-Positionen bei bärischen Farben um. |
| `ShortCandleType` | 4-Stunden-Kerzen | Datentyp für den Short-seitigen Indikator. |
| `ShortSsp` | 9 | SilverTrend-Lookback-Länge für den Short-Filter. |
| `ShortRisk` | 3 | Risiko-Multiplikator für den Short-Filter. |
| `ShortSignalBar` | 1 | Offset für die Auswertung von Short-Signalen. Muss ≥ 1 sein. |
| `EnableShortEntries` | true | Schaltet das Öffnen von Short-Positionen um. |
| `EnableShortExits` | true | Schaltet das Schließen von Short-Positionen bei bullischen Farben um. |
| `Volume` | 1 | Basisordervolumen für Einstiege. |

## Implementierungshinweise

- Signale werden erst ausgewertet, nachdem sowohl der Indikator als auch die Farbhistorie genug Daten enthalten (`SignalBar + 1` Werte). Dies spiegelt die `BarsCalculated`-Prüfungen aus dem MQL-Experten wider.
- Der benutzerdefinierte Indikator stellt dezimale Farbwerte bereit, anstatt rohe Buffer-Daten zu kopieren. Dank der `Bind`-High-Level-API sind keine direkten `GetValue`-Aufrufe erforderlich.
- Wenn Long- und Short-Kerzentypen identisch sind, werden absichtlich zwei Abonnements erstellt, um die Parametersätze isoliert zu halten. Dies entspricht dem Doppelhandle-Verhalten im ursprünglichen Berater.
- Stop-Loss-, Take-Profit-, Abweichungs- und Margin-Management-Optionen aus dem Quellskript werden nicht repliziert. Sie können StockSharp-Risikoregeln (z.B. `StopLossRule`) hinzufügen, wenn ein ähnliches Verhalten benötigt wird.

## Verwendungstipps

- Optimieren Sie `LongSsp`, `ShortSsp` und entsprechende Risikowerte separat, um die Ausbruchsschwellenwerte an jedes Marktregime anzupassen.
- Wenn Sie das ursprüngliche "Signal auf vorheriger Bar"-Verhalten emulieren möchten, halten Sie `SignalBar` bei `1`. Größere Werte zwingen die Strategie, zusätzliche Bars zu warten, bevor sie reagiert.
- Kombinieren Sie die Strategie mit risikokontrollen auf Portfolio-Ebene oder Zeitfiltern, wenn Sie auf mehreren Instrumenten laufen, da der SilverTrend-Farbwechsel in trendlosen Märkten häufige Regimewechsel produzieren kann.
