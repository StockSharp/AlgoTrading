# MACD + Stochastic Estrategia de filtro de tendencias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el comportamiento del asesor experto MetaTrader de la carpeta `MQL/7604`. El script original se basaba en un oscilador personalizado que producía buffers verdes y rojos. En la práctica, los números `(15, 3, 3)` coinciden con un oscilador estocástico clásico, por lo tanto, el puerto StockSharp utiliza el indicador incorporado `Stochastic` para la confirmación de la señal, mientras que MACD y un filtro de tendencia EMA administran la dirección.

La estrategia opera tanto en largo como en corto. Espera un cruce estocástico en la dirección de la operación, requiere que el histograma MACD cruce su línea de señal a una distancia suficiente de cero y exige que la pendiente EMA concuerde con la entrada. La gestión de riesgos refleja la versión MQL: un stop-loss fijo, una toma de ganancias y un trailing stop basado en puntos que refuerza el nivel de protección tan pronto como la operación genera ganancias.

## Indicadores

- **MovingAverageConvergenceDivergenceSignal** con parámetros `fast = 12`, `slow = 26`, `signal = 9`. El histograma MACD debe cruzar su línea de señal mientras permanece por debajo de cero para configuraciones largas y por encima de cero para configuraciones cortas. Los umbrales adicionales (`MacdOpenLevel`, `MacdCloseLevel`) imponen una distancia absoluta mínima desde la línea cero.
- **Stochastic** oscilador con `(Length = 15, KPeriod = 3, DPeriod = 3)`. La línea %K desempeña el papel de amortiguador "verde" y debe estar por encima de %D para operaciones largas (por debajo para operaciones cortas). El mismo cruce se utiliza para salir de posiciones.
- **ExponentialMovingAverage** con período `26`. El EMA proporciona un filtro direccional: para una operación larga, el valor actual EMA debe estar por encima del EMA de la barra anterior, y a la inversa, para una operación corta.

## Lógica de entrada

1. **Configuración larga**
   - Stochastic %K > %D en la vela cerrada actual.
   - MACD histograma < 0 y > línea de señal en la barra actual.
   - MACD histograma <línea de señal en la barra anterior (es decir, cruce alcista ahora).
   - `|MACD| > MacdOpenLevel * price_step`.
   - EMA en aumento ({PH001}} actual > {PH002}} anterior).
2. **Configuración corta**
   - Stochastic %K < %D en la vela actual.
   - MACD histograma > 0 y < línea de señal en la barra actual.
   - MACD histograma > línea de señal en la barra anterior (cruce bajista ahora).
   - `MACD > MacdOpenLevel * price_step`.
   - EMA cayendo ({PH001}} actual < anterior EMA).

Si la cuenta ya tiene una posición, no se generan nuevas órdenes hasta que se cierre la operación abierta.

## Salir de la lógica

Mientras una posición está abierta, la estrategia aplica continuamente:

- **Salida del indicador**
  - Las posiciones largas se cierran cuando `%K < %D`, MACD > 0, MACD < señal, el MACD anterior estaba por encima de su señal y el histograma absoluto excede `MacdCloseLevel * price_step`.
  - Las posiciones cortas se cierran cuando `%K > %D`, MACD < 0, MACD > señal, el MACD anterior estaba por debajo de su señal y `|MACD| > MacdCloseLevel * price_step`.
- **Stop-loss**: configurado por `StopLossPoints`, convertido en unidades de precio a través del `PriceStep` del instrumento.
- **Take-profit**: `TakeProfitPoints` multiplicado por `PriceStep`.
- **Trailing stop**: una vez que la ganancia excede `TrailingStopPoints * PriceStep`, el nivel de parada aumenta (para largos) o baja (para cortos) para que la operación siempre bloquee al menos esa cantidad de ganancias.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño del pedido en lotes | `0.1` |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos | `10` |
| `StopLossPoints` | Distancia de stop-loss en puntos | `50` |
| `TrailingStopPoints` | Distancia del trailing stop en puntos | `5` |
| `MacdOpenLevel` | Valor mínimo absoluto MACD para entradas | `3` |
| `MacdCloseLevel` | Valor mínimo absoluto MACD para salidas | `2` |
| `MacdFastPeriod` | EMA rápida longitud dentro de MACD | `12` |
| `MacdSlowPeriod` | Longitud lenta de EMA dentro de MACD | `26` |
| `MacdSignalPeriod` | MACD señal EMA longitud | `9` |
| `EmaPeriod` | EMA período para el filtro de tendencias | `26` |
| `StochasticLength` | Stochastic ventana retrospectiva | `15` |
| `StochasticKPeriod` | %K suavizado | `3` |
| `StochasticDPeriod` | %D suavizado | `3` |
| `CandleType` | Plazo utilizado para los cálculos | `15m` |

## Notas

- Todos los cálculos utilizan únicamente velas terminadas, que coinciden con el bucle `start()` en el EA original.
- El `PriceStep` suministrado por el instrumento define un punto. Cuando la seguridad no expone un paso, la estrategia vuelve a `1`.
- El código se basa exclusivamente en el nivel alto API de StockSharp: los indicadores están vinculados a través de `SubscribeCandles().BindEx(...)`, no se crean buffers de historial manuales y los pedidos usan `BuyMarket`/`SellMarket` como en la versión MQL.
