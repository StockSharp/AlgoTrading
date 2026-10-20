# Big Mover Catcher

[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Reglas de negociación

Usa cierres terminados de un minuto y bandas de Bollinger (40 barras, dos desviaciones poblacionales). Sin posición, compra `Volume` por encima de la banda superior o vende `Volume` por debajo de la inferior. `TradeDirection = null` permite ambas direcciones; `Buy` o `Sell` limita entradas.

Los cinco filtros están activos por defecto y deben confirmar: RSI(14) estrictamente mayor de 50 para largo/menor de 50 para corto; ADX(28) al menos 20; `100 * ATR(14) / Close >= 0.05`; cierre sobre/bajo EMA(350); histograma MACD(12,26,9) positivo/negativo. Cada filtro se desactiva por separado. Solo los indicadores activos deben estar formados; EMA desactivada no impone esperar 350 barras.

Cierra todo el largo con cierre completo en/bajo la banda media, o todo el corto en/sobre ella. `IsForceTakeProfitEnabled` añade salida si el cuerpo favorable dividido por apertura alcanza `ForceTakeProfitPercent` por ciento, por defecto 5. Se mide el cuerpo actual, no la ganancia desde entrada; está desactivado por defecto. La salida tiene prioridad sin reversión en la misma vela.

El stop nativo local a mercado usa `StopLossPercent` por ciento desde la entrada ejecutada, predeterminado 2. Una orden activa evita duplicados.

La protección local nativa usa velas y actualizaciones de precio suscritas del instrumento. Las distancias parten de los precios ejecutados. Las salidas son a mercado y dependen del mercado disponible y del paso de precio.

## Parámetros

| Parámetro | Predeterminado |
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
