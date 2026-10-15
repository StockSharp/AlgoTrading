# Estrategia Elderv30aug05v
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Elderv30aug05v es una adaptación directa de los MetaTrader 4 asesores expertos con el mismo nombre. Combina señales de dos filtros MACD calculados sobre velas horarias y dos osciladores estocásticos calculados sobre velas de 15 minutos. La ejecución de operaciones y la gestión de salida se realizan en velas de un minuto para replicar la lógica tick por tick del script MQL original. La estrategia abre como máximo una posición a la vez y se basa en paradas dinámicas en lugar de órdenes fijas de toma de ganancias.

## Indicadores y datos
- **Primario MACD** (`13/30/9`, velas horarias). Una señal larga requiere que el histograma tenga una pendiente ascendente mientras el valor anterior permanece por debajo de cero.
- **Secundario MACD** (`14/56/9`, velas horarias). Una señal corta requiere que el histograma tenga una pendiente descendente mientras el valor anterior permanece por encima de cero.
- **Oscilador estocástico rápido** (`%K=2`, `%D=3`, suavizado=3 velas de 15 minutos). Las entradas largas exigen que la línea %K esté por debajo del techo configurado (predeterminado 36) y aumente en relación con la barra anterior.
- **Oscilador estocástico lento** (`%K=1`, `%D=3`, suavizado=3 velas de 15 minutos). Las entradas cortas requieren que la línea %K esté por encima del piso configurado (predeterminado 66) y decreciente en relación con la barra anterior.
- **Las velas de un minuto** proporcionan los datos de confirmación para las comprobaciones de ruptura y gestionan los trailingstops.

Todos los indicadores procesan solo velas terminadas hasta `SubscribeCandles().Bind()/BindEx()` para seguir las pautas de alto nivel StockSharp API.

## Reglas de entrada
### Configuración larga
1. El valor principal MACD está por encima de su lectura anterior y la lectura anterior es negativa.
2. El estocástico rápido %K está por debajo de `LongStochasticThreshold` (36 predeterminado) y por encima de su valor anterior.
3. El cierre de la vela actual de un minuto es mayor que el máximo de la vela anterior de un minuto.

### Configuración corta
1. El valor secundario MACD está por debajo de su lectura anterior y la lectura anterior es positiva.
2. El estocástico lento %K está por encima de `ShortStochasticThreshold` (predeterminado 66) y por debajo de su valor anterior.
3. El cierre de la vela actual de un minuto es más bajo que el mínimo de la vela anterior de un minuto.

Sólo puede haber una posición abierta. Si aparece una nueva señal mientras una posición está activa, se ignora hasta que la posición se cierre mediante stop-loss o lógica de seguimiento.

## Reglas de salida
- **Stop-loss inicial**: Al ingresar, la estrategia almacena el precio de entrada más/menos `LongStopLoss` o `ShortStopLoss` multiplicado por el instrumento `PriceStep`. Si no se proporciona `PriceStep`, se utiliza un respaldo de `0.0001`.
- **Trailing stop**: una vez que el precio se mueve a favor de la operación en al menos `LongTrailingStop` o `ShortTrailingStop` puntos (nuevamente multiplicado por `PriceStep`), el precio stop almacenado se desplaza detrás del mercado. Para operaciones largas, el stop sigue el cierre menos la distancia de seguimiento y solo se mueve hacia arriba. Para operaciones cortas, el stop sigue el cierre más la distancia y solo se mueve hacia abajo.
- Cuando el rango de velas toca el precio stop almacenado, la posición se cierra en el mercado.

No se utiliza ningún nivel fijo de obtención de beneficios, lo que refleja el comportamiento original de MetaTrader.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `0.1` | Volumen comercial enviado a `BuyMarket`/`SellMarket`. |
| `LongStopLoss` | `17` | Larga distancia de stop-loss en puntos. |
| `ShortStopLoss` | `46` | Distancia corta de stop-loss en puntos. |
| `LongTrailingStop` | `18` | Distancia de seguimiento para posiciones largas. |
| `ShortTrailingStop` | `22` | Distancia de seguimiento para posiciones cortas. |
| `LongStochasticThreshold` | `36` | Valor %K estocástico rápido máximo para entradas largas. |
| `ShortStochasticThreshold` | `66` | Valor mínimo de %K estocástico lento para entradas cortas. |
| `BaseCandleType` | `TimeFrame(1m)` | Serie de velas utilizada para la lógica de ejecución. |
| `StochasticCandleType` | `TimeFrame(15m)` | Serie de velas para ambos osciladores estocásticos. |
| `MacdCandleType` | `TimeFrame(1h)` | Serie de velas para ambos filtros MACD. |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | `13 / 30 / 9` | Períodos para el MACD principal. |
| `AltMacdFastPeriod` / `AltMacdSlowPeriod` / `AltMacdSignalPeriod` | `14 / 56 / 9` | Períodos para la secundaria MACD. |
| `StochasticFastKPeriod` / `StochasticFastDPeriod` / `StochasticFastSmooth` | `2 / 3 / 3` | Parámetros del estocástico rápido. |
| `StochasticSlowKPeriod` / `StochasticSlowDPeriod` / `StochasticSlowSmooth` | `1 / 3 / 3` | Parámetros para el estocástico lento. |

## Notas
- La estrategia funciona con cualquier instrumento que proporcione velas de nivel minuto y un `PriceStep` válido.
- Las paradas finales se mantienen internamente; no se registran órdenes de protección por parte del mercado.
- La lógica procesa solo velas terminadas para evitar repintar y coincide con la implementación MQL que se basó en barras completas.

## Guión Original
- **Fuente**: `MQL/7674/Elderv30aug05v.mq4`
- **Plataforma**: MetaTrader 4 asesores expertos.
