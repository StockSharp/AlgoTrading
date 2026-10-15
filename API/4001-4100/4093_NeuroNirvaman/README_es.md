# Estrategia Neuro Nirvaman MQ4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Neuro Nirvaman MQ4** es una fiel adaptación del MetaTrader 4 asesor experto `NeuroNirvaman.mq4`. El robot original combina un filtro Laguerre personalizado aplicado al componente +DI del indicador ADX con un detector de ruptura SilverTrend. Tres perceptrones evalúan estas entradas y un supervisor decide si comprar o vender. La versión StockSharp refleja ese flujo de trabajo y ejecuta una posición a la vez, recalculando su lógica solo en velas completamente cerradas.

## Cómo funciona la estrategia
1. **Feed de datos de mercado**: la estrategia se suscribe a una única serie de velas definida por `CandleType` y procesa solo `Finished` velas. No evalúa eventos intrabar, replicando la verificación `Time[0]` utilizada en MT4.
2. **Suavizado Laguerre +DI**: cuatro indicadores `AverageDirectionalIndex` proporcionan valores +DI que se envían a través de un filtro Laguerre (`LaguerrePlusDiState`) utilizando la gamma original de 0,764. El filtro produce valores de oscilador en el rango `[0, 1]` y cada flujo tiene su propio período ADX y ancho de zona neutral (`Laguerre*Distance`).
3. **Puerto SilverTrend**: dos objetos `SilverTrendState` reproducen la lógica `Sv2.mq4`. Realizan un seguimiento del máximo más alto y del mínimo más bajo para `SSP` velas, reducen el canal con la constante `Kmax = 50.6` y devuelven `1` para una tendencia alcista o `-1` para una tendencia bajista. Las profundidades de retrospectiva están controladas por `SilverTrend1Length` y `SilverTrend2Length`.
4. **Perceptrones** –
   - *Perceptron #1* mezcla la primera activación de Laguerre con el primer swing de SilverTrend usando los pesos `X11 - 100` y `X12 - 100`.
   - *Perceptron #2* combina la segunda activación de Laguerre con el segundo swing de SilverTrend y pesa `X21 - 100` y `X22 - 100`.
   - *Perceptron #3* evalúa la tercera y cuarta activaciones de Laguerre ponderadas por `X31 - 100` y `X32 - 100`.
Cada activación de Laguerre se cuantifica en `-1`, `0` o `1` dependiendo de su distancia del nivel de equilibrio 0,5.
5. **Supervisor (`Pass`)** – El supervisor reproduce la función MQL `Supervisor()`:
   - `Pass = 3`: requiere `Perceptron #3 > 0`. Si también es `Perceptron #2 > 0`, la estrategia compra utilizando el segundo conjunto TP/SL; de lo contrario, si es `Perceptron #1 < 0`, se vende utilizando el primer conjunto TP/SL.
   - `Pass = 2`: un `Perceptron #2` positivo abre un largo con el segundo conjunto TP/SL, mientras que cualquier valor no positivo abre un corto con el primer conjunto.
   - `Pass = 1`: un `Perceptron #1` negativo abre un corto, en caso contrario se abre un largo. Ambas ramas utilizan el primer conjunto TP/SL.
6. **Gestión de pedidos y riesgos**: las inscripciones se envían con `BuyMarket` o `SellMarket` usando `TradeVolume`. Los niveles de toma de ganancias y límite de pérdidas se calculan como `entry ± points * PriceStep`. Debido a que StockSharp realiza órdenes de mercado puras, las salidas protectoras se simulan verificando los máximos y mínimos de las velas, exactamente como se activarían las órdenes TP/SL del lado del corredor en MT4.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | plazo de 15 minutos | Tipo de vela procesada por la estrategia. |
| `TradeVolume` | `decimal` | 0.1 | Volumen de pedidos en lotes. |
| `SilverTrend1Length` | `int` | 7 | Longitud retrospectiva para el primer cálculo de SilverTrend (SSP). |
| `Laguerre1Period` | `int` | 14 | ADX período para la primera transmisión de Laguerre. |
| `Laguerre1Distance` | `decimal` | 0 | Ancho de la zona neutral (porcentaje) alrededor de 0,5 para el arroyo Laguerre #1. |
| `X11`, `X12` | `decimal` | 100 | Pesos utilizados dentro del perceptrón n.° 1 (el código resta 100 antes de aplicarlos). |
| `TakeProfit1`, `StopLoss1` | `decimal` | 100 / 50 | Distancias de protección en puntos para el primer perfil de riesgo y todas las operaciones cortas. |
| `SilverTrend2Length` | `int` | 7 | Longitud retrospectiva para el segundo cálculo de SilverTrend. |
| `Laguerre2Period` | `int` | 14 | ADX período para la segunda transmisión de Laguerre. |
| `Laguerre2Distance` | `decimal` | 0 | Ancho de la zona neutral (porcentaje) alrededor de 0,5 para el arroyo Laguerre #2. |
| `X21`, `X22` | `decimal` | 100 | Pesos utilizados dentro del perceptrón n.° 2. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 100 / 50 | Distancias de protección en puntos para el segundo perfil de riesgo. |
| `Laguerre3Period`, `Laguerre4Period` | `int` | 14 | ADX períodos para la tercera y cuarta transmisión de Laguerre. |
| `Laguerre3Distance`, `Laguerre4Distance` | `decimal` | 0 | Anchos de la zona neutral (porcentaje) para la tercera y cuarta corriente Laguerre. |
| `X31`, `X32` | `decimal` | 100 | Pesos utilizados dentro del perceptrón n.° 3. |
| `Pass` | `int` | 3 | Rama supervisora que selecciona qué perceptrones pueden activar operaciones. |

## Notas de uso
- Los pesos predeterminados de `100` neutralizan la entrada del perceptrón correspondiente. Aleje los pesos de 100 para crear señales significativas.
- SilverTrend comienza a devolver `±1` una vez que se recolectan suficientes velas. Hasta entonces, las salidas del perceptrón pueden permanecer en cero, emulando el comportamiento de MT4 donde `iCustom` devuelve cero antes de que los buffers estén listos.
- Las comprobaciones de obtención de beneficios y limitación de pérdidas se basan en los extremos de las velas; Si se producen picos dentro de la vela entre barras, la simulación puede diferir ligeramente de la ejecución por parte del corredor.
- Sólo puede existir una posición a la vez. Una nueva señal se ignora hasta que TP, SL o una decisión contraria cierren la posición actual.
- Ajuste `CandleType` para reflejar el período del gráfico utilizado por la configuración MT4 original (por ejemplo, M15 o H1) para mantener constante la escala del indicador.
