# MACD Estrategia no tan de muestra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia MACD Not So Sample es una conversión del asesor experto MetaTrader *MACD_Not_So_Sample*. El robot original opera
un gráfico EURUSD de 4 horas que utiliza cruces MACD confirmados por un filtro de tendencia EMA, combinado con grandes niveles de toma de ganancias y un
parada de seguimiento. La versión StockSharp mantiene la misma estructura: el histograma MACD debe ser negativo y cruzar por encima de su señal
línea para una entrada larga, mientras que un histograma positivo que cruza por debajo de la señal produce una entrada corta. Una tendencia EMA debe confirmar la
dirección antes de abrir cualquier posición.

Todas las funciones de administración de dinero se implementan en StockSharp: la estrategia establece un objetivo de obtención de ganancias configurable, administra un
trailing stop una vez que el precio viaja lo suficientemente lejos y cierra las operaciones cuando el MACD cruza en la dirección opuesta con suficiente
fuerza. El puerto utiliza indicadores StockSharp y suscripciones de velas de alto nivel para que todos los cálculos se realicen en el cuarto semestre finalizado.
velas, reflejando el comportamiento de MetaTrader.

## Lógica comercial
1. Suscríbase al período definido por `CandleType` (el valor predeterminado es velas de 4 horas) y procese solo velas terminadas.
2. Alimente un indicador `MovingAverageConvergenceDivergenceSignal` con los `FastPeriod`, `SlowPeriod` y
`SignalPeriod`. El indicador proporciona tanto la línea MACD como la línea de señal.
3. Calcule un filtro de tendencia EMA con longitud `TrendPeriod`. Su pendiente determina si se permiten entradas largas o cortas.
4. Convierta los umbrales basados en pips (`MacdOpenLevelPips`, `MacdCloseLevelPips`, `TakeProfitPips`, `TrailingStopPips`) a absolutos
distancias de precios utilizando el tamaño del pip del instrumento.
5. Cuando no existe ningún puesto:
   - Abra una posición **larga** si el MACD está por debajo de cero, el valor actual está por encima del valor de la señal, el MACD anterior estaba por debajo
la señal anterior, el EMA está aumentando y la magnitud MACD excede `MacdOpenLevelPips`.
   - Abra una posición **corta** si el MACD está por encima de cero, el valor actual está por debajo del valor de la señal, el MACD anterior estaba por encima
la señal anterior, el EMA está cayendo, y la magnitud MACD excede `MacdOpenLevelPips`.
6. Mientras mantiene una posición larga:
   - Cierre la operación cuando MACD se vuelva positivo, cruce por debajo de la señal y su magnitud supere `MacdCloseLevelPips`.
   - Salga anticipadamente si el precio alcanza la toma de ganancias configurada o si se supera el nivel del trailing stop.
7. Mientras mantiene una posición corta:
   - Cierre la operación cuando MACD se vuelva negativo, cruce por encima de la señal y su magnitud supere `MacdCloseLevelPips`.
   - Salga temprano si el precio alcanza el objetivo de obtención de ganancias o el trailing stop.
8. El trailing stop se activa sólo después de que el precio supera el umbral en `TrailingStopPips` y luego bloquea las ganancias en
siguiendo los extremos de las velas posteriores.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `47` | Longitud rápida de EMA utilizada dentro del cálculo de MACD. |
| `SlowPeriod` | `int` | `166` | Longitud lenta de EMA utilizada dentro del cálculo de MACD. |
| `SignalPeriod` | `int` | `11` | EMA longitud de la línea de señal MACD. |
| `TrendPeriod` | `int` | `8` | Longitud del filtro de tendencia EMA. |
| `MacdOpenLevelPips` | `decimal` | `1` | Magnitud mínima de MACD (en pips) necesaria para abrir una posición. |
| `MacdCloseLevelPips` | `decimal` | `3` | Magnitud mínima MACD (en pips) necesaria para cerrar una posición. |
| `TakeProfitPips` | `decimal` | `550` | Distancia de obtención de beneficios medida en pips. |
| `TrailingStopPips` | `decimal` | `19` | Distancia del trailing-stop medida en pips. Un valor de `0` deshabilita el seguimiento. |
| `TradeVolume` | `decimal` | `1` | Volumen neto utilizado para las entradas al mercado. |
| `CandleType` | `DataType` | plazo de 4 horas | Serie de velas procesadas por la estrategia. |
| `RequiredSecurityCode` | `string` | `EURUSD` | Código de seguridad que debe coincidir con el instrumento seleccionado, imitando la verificación MetaTrader. |

## Diferencias con el experto MetaTrader original
- MetaTrader gestiona pedidos individuales y números mágicos. StockSharp trabaja con posiciones netas, por lo que la conversión cierra el
exposición actual y abre una nueva en lugar de hacer malabarismos con varios tickets.
- El código original usaba `AccountFreeMargin` para dimensionar las posiciones dinámicamente. El puerto StockSharp expone un simple `TradeVolume`
parámetro y documentos que los usuarios deben configurar el tamaño de la posición externamente.
- Los ajustes de stop-loss utilizan los extremos de las velas de StockSharp en lugar de modificar las órdenes existentes. Las salidas todavía ocurren en la primera
vela que viola el trailing stop, produciendo un comportamiento muy cercano a la lógica MetaTrader.
- Todos los cálculos de indicadores se basan en StockSharp clases de indicadores vinculadas a través de `SubscribeCandles`, sin llamadas directas a
Funciones `iMACD` o `iMA`.

## Notas de uso
- Asigne el instrumento deseado antes de iniciar la estrategia. Si el código del instrumento no coincide con `RequiredSecurityCode` el
La estrategia se detiene inmediatamente para evitar un despliegue accidental en el mercado equivocado.
- `TradeVolume` se copia en `Strategy.Volume` durante `OnStarted`, por lo que los métodos auxiliares (`BuyMarket`, `SellMarket`) siempre usan el
tamaño configurado.
- Los trailingstops solo se activan después de que el precio avanza más allá de la distancia configurada; Hasta entonces la estrategia dependerá de la
MACD objetivo de cruce y toma de ganancias para salidas.
- Agregar la estrategia a un gráfico genera velas, ambos indicadores y operaciones ejecutadas para que se pueda validar la lógica cruzada.
visualmente.

## Indicadores
- `MovingAverageConvergenceDivergenceSignal` (MACD línea y línea de señal).
- `ExponentialMovingAverage` (filtro de tendencias).
