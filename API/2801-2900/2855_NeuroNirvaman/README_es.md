# Estrategia Neuro Nirvaman
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Neuro Nirvaman es una conversión directa del asesor experto MetaTrader 5 *NeuroNirvamanEA*. Recrea el árbol de decisión basado en perceptrones de la implementación MQL original combinando cuatro indicadores de dirección positiva (+DI) suavizados por Laguerre con dos detectores de oscilación SilverTrend. La estrategia trabaja con velas terminadas y envía órdenes de mercado con niveles de take-profit y stop-loss dinámicos definidos en puntos. No se aplica trailing stop, promedio ni piramidación – solo puede existir una posición en cualquier momento.

## Entradas de mercado e indicadores
- **AverageDirectionalIndex (4 instancias)** – cada instancia está configurada con su propio período. La estrategia lee el componente +DI y lo pasa a través de un filtro de Laguerre para obtener valores de oscilador suaves en el rango `[0, 1]`.
- **LaguerrePlusDiState** – un helper interno que reproduce la lógica del indicador personalizado `laguerre_plusdi.mq5`, incluyendo el suavizado Laguerre de cuatro etapas y la normalización `CU / (CU + CD)`.
- **SilverTrendState (2 instancias)** – un porto fiel de la lógica `silvertrend_signal.mq5`. Evalúa las últimas 10 velas (`SSP = 9`) para detectar puntos de ruptura, y emite `1` en flechas bajistas, `-1` en flechas alcistas, o `0` cuando no hay flecha presente.
- **Stream de velas** – la estrategia se suscribe a un solo marco temporal seleccionado a través de `CandleType` y procesa solo las velas terminadas.

## Lógica de trading
1. **Preparación de señales**
   - Cada valor de Laguerre se traduce en una activación discreta mediante el helper `ComputeTensionSignal`: valores por encima de `0.5 + distance/100` generan `-1`, por debajo de `0.5 - distance/100` generan `1`, y la zona neutral produce `0`.
   - Los swings de SilverTrend se actualizan en cada vela. Los parámetros de riesgo (`Risk1`, `Risk2`) reducen o amplían el canal de soporte/resistencia exactamente como en el indicador MQL.
2. **Perceptrones**
   - **Perceptrón 1** mezcla la primera activación de Laguerre con el primer swing de SilverTrend usando pesos `X11 - 100` y `X12 - 100`.
   - **Perceptrón 2** mezcla la segunda activación de Laguerre con el segundo swing de SilverTrend usando pesos `X21 - 100` y `X22 - 100`.
   - **Perceptrón 3** trabaja en las tercera y cuarta activaciones de Laguerre con pesos `X31 - 100` y `X32 - 100`.
3. **Supervisor (parámetro Pass)**
   - `Pass = 3`: requiere `Perceptrón 3 > 0`. Si también `Perceptrón 2 > 0`, la estrategia compra usando `TakeProfit2` / `StopLoss2`. De lo contrario, si `Perceptrón 1 < 0`, vende usando `TakeProfit1` / `StopLoss1`.
   - `Pass = 2`: cuando `Perceptrón 2 > 0`, se abre una posición larga con el segundo conjunto de límites de riesgo. Si `Perceptrón 2 <= 0`, se abre un corto con el primer conjunto de límites.
   - `Pass = 1`: cuando `Perceptrón 1 < 0`, la estrategia vende usando el primer conjunto de riesgo. De lo contrario, va largo usando los mismos ajustes de riesgo.
4. **Gestión de órdenes**
   - Las entradas se ejecutan con `BuyMarket` o `SellMarket` y usan el parámetro `TradeVolume` como tamaño de lote.
   - Los niveles de take-profit y stop-loss se calculan desde el precio de cierre de la vela de señal: `entry ± points * PriceStep`. Se monitorean en cada vela terminada a través de verificaciones de alto/bajo, emulando las órdenes de protección originales de MT5.
   - Las nuevas señales se ignoran mientras haya una posición activa; solo cuando la posición se cierra se evalúan nuevos trades.

## Parámetros
| Nombre | Tipo | Valor predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 15 minutos | Tipo de vela usado para los cálculos. |
| `TradeVolume` | `decimal` | 0.1 | Volumen de la posición en lotes. |
| `Risk1`, `Risk2` | `int` | 3 / 9 | Factores de riesgo SilverTrend que definen el ancho del canal. |
| `Laguerre1Period` – `Laguerre4Period` | `int` | 14 | Longitud ADX para cada stream de suavizado Laguerre. |
| `Laguerre1Distance` – `Laguerre4Distance` | `decimal` | 0 | Distancia en porcentaje (0–100) alrededor del umbral 0.5 que define la zona neutral. |
| `X11`, `X12`, `X21`, `X22`, `X31`, `X32` | `decimal` | 100 | Coeficientes de peso; el código MQL resta 100 antes de aplicarlos. |
| `TakeProfit1`, `StopLoss1`, `TakeProfit2`, `StopLoss2` | `int` | 100 / 50 | Distancias de protección expresadas en puntos. |
| `Pass` | `int` | 3 | Modo supervisor que selecciona la combinación de perceptrones usados para trading. |

## Notas de uso
- Los pesos predeterminados (`100`) neutralizan los perceptrones. Para activar la estrategia, ajuste los pesos lejos de `100` para que los perceptrones puedan producir salidas distintas de cero.
- La implementación de SilverTrend respeta la lógica de conteo de alertas original (sin alertas) y mantiene el estado entre velas, por lo que las señales se alinean con la versión MT5.
- Dado que los niveles de take-profit y stop-loss se simulan internamente, se usa el alto/bajo de cada vela completada para verificar los hits de objetivo. Los picos intrabarra entre ticks no se modelan.
- La estrategia es de símbolo único y no gestiona múltiples instrumentos. Adjúntela al instrumento deseado y configure la serie de velas en consecuencia.
- Solo se permiten posiciones largas o cortas a la vez; revertir la posición fuerza primero una salida completa.

## Implementación
1. Compilar la solución y ejecutar la estrategia desde el lanzador de muestras de StockSharp o incluirla en un proyecto personalizado.
2. Elegir el instrumento, asignar la serie de velas, y configurar los pesos del perceptrón más los parámetros de riesgo.
3. Iniciar la estrategia y monitorear los trades usando el gráfico creado automáticamente (los indicadores Laguerre y los propios deals se agregan al área).
4. Las optimizaciones se pueden ejecutar a través de los metadatos de parámetros integrados (`SetCanOptimize`) si se desea.
