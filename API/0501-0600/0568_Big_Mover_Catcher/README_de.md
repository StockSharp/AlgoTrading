# Big Mover Catcher

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Handelsregeln

Abgeschlossene Minutenschlusskurse und Bollinger-Bänder über 40 Kerzen mit zwei Populationsstandardabweichungen bilden die Basis. Ohne Position wird oberhalb des oberen Bandes `Volume` gekauft, unterhalb des unteren `Volume` verkauft. `TradeDirection = null` erlaubt beide Seiten, `Buy` oder `Sell` beschränkt Einstiege.

Alle fünf Filter sind standardmäßig aktiv und müssen bestätigen: RSI(14) strikt über 50 für Long/unter 50 für Short; ADX(28) mindestens 20; `100 * ATR(14) / Close >= 0.05`; Schluss über/unter EMA(350); MACD(12,26,9)-Histogramm positiv/negativ. Jeder Filter ist separat abschaltbar. Nur aktive Indikatoren müssen gebildet sein; ohne EMA entfallen 350 Kerzen Wartezeit.

Die gesamte Long-Position wird bei Schluss am/unter dem Mittelband, die Short-Position am/darüber geschlossen. Optional löst `IsForceTakeProfitEnabled` auch aus, wenn der günstige Kerzenkörper geteilt durch die Eröffnung mindestens `ForceTakeProfitPercent` Prozent erreicht, standardmäßig 5. Das misst den aktuellen Körper, keinen Einstiegsgewinn, und ist standardmäßig aus. Ausstiege haben Vorrang; keine Umkehr auf derselben Kerze.

Der native lokale Marktstop liegt `StopLossPercent` Prozent vom ausgeführten Einstieg entfernt, standardmäßig 2. Eine aktive Order verhindert Duplikate.

Der native lokale Schutz verwendet Kerzen und abonnierte Preisaktualisierungen des Instruments. Abstände beziehen sich auf tatsächliche Einstiegsausführungen. Ausstiege sind Marktorders; der Ausführungspreis hängt vom verfügbaren Markt und Preisraster ab.

## Parameter

| Parameter | Standardwert |
|---|---|
| `BollingerPeriod` | `40` |
| `BollingerDeviation` | `2` |
| `IsRsiFilterEnabled` | `true` |
| `RsiPeriod` | `14` |
| `RsiLongThreshold` | `50` |
| `RsiShortThreshold` | `50` |
| `IsAdxFilterEnabled` | `true` |
| `AdxPeriod` | `28` |
| `MinimumAdx` | `20` |
| `IsAtrFilterEnabled` | `true` |
| `AtrPeriod` | `14` |
| `MinimumAtrPercent` | `0.05` |
| `IsEmaFilterEnabled` | `true` |
| `EmaPeriod` | `350` |
| `IsMacdFilterEnabled` | `true` |
| `MacdFast` | `12` |
| `MacdSlow` | `26` |
| `MacdSignal` | `9` |
| `TradeDirection` | `null` |
| `StopLossPercent` | `2` |
| `IsForceTakeProfitEnabled` | `false` |
| `ForceTakeProfitPercent` | `5` |
| `CandleType` | `1m` |
