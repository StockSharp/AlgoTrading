# Estrategia de escape de reversión a la media
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Escape es una StockSharp adaptación del MetaTrader 4 asesores expertos `escape.mq4`. El robot original negocia un gráfico de cinco minutos y reacciona a las oportunidades de reversión a la media: compra cuando el precio cae por debajo de un promedio móvil corto y vende cuando el precio sube por encima de otro promedio rápido. Cada posición está protegida por un take-profit y un stop-loss a distancia fija expresados ​​en MetaTrader puntos. La implementación de C# mantiene la misma lógica minimalista al tiempo que expone todas las distancias ajustables como parámetros de estrategia.

## Lógica de trading
1. **Inicialización**
   - Suscríbete a la serie `CandleType` configurable (velas de cinco minutos por defecto).
   - Cree dos indicadores `SimpleMovingAverage` con longitudes 5 y 4 que se alimenten con precios de apertura de velas.
   - Calcular el equivalente de MetaTrader `Point` de `Security.PriceStep`; este valor se reutiliza para convertir distancias estilo pip en precios absolutos.

2. **Procesamiento por vela**
   - Sólo las velas terminadas se procesan a través de `SubscribeCandles(...).WhenCandlesFinished(ProcessCandle)`.
   - La estrategia primero verifica si una posición existente alcanzó su límite de pérdidas o toma de ganancias comparando el máximo/mínimo de la vela con los niveles de salida registrados. Cuando se supera un nivel, la posición se cierra con una orden de mercado y se evitan órdenes de salida duplicadas mediante banderas internas.
   - Si la cuenta es plana, los valores anteriores de las dos SMA están disponibles, se permite el comercio y la cartera tiene suficiente capital (`Portfolio.CurrentValue >= MinimumMarginPerLot * TradeVolume`), la estrategia evalúa las entradas:
     * **Entrada larga**: el cierre actual está por debajo de los 5 períodos anteriores SMA de aperturas.
     * **Entrada breve**: el cierre actual está por encima de los 4 períodos anteriores SMA de aperturas.
   - Cuando se activa una señal, los niveles de stop-loss y take-profit se calculan a partir del cierre de la vela utilizando las distancias de puntos configuradas y se almacenan para un seguimiento posterior.

3. **Gestión de riesgos**
   - `TradeVolume` define el tamaño del lote de cada orden de mercado.
   - `MinimumMarginPerLot` se aproxima al cheque `AccountFreeMargin` de MetaTrader. Si el valor de la cartera disponible es demasiado pequeño, la entrada se omite y se registra un mensaje de diagnóstico.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `LongTakeProfitPoints` | `10` | Distancia de obtención de beneficios para posiciones largas en MetaTrader puntos. Establezca en `0` para desactivar el objetivo. |
| `ShortTakeProfitPoints` | `10` | Distancia de obtención de beneficios para posiciones cortas en MetaTrader puntos. Establezca en `0` para desactivar el objetivo. |
| `LongStopLossPoints` | `1000` | Distancia de stop-loss para posiciones largas en MetaTrader puntos. Establezca en `0` para desactivar la parada de protección. |
| `ShortStopLossPoints` | `1000` | Distancia de stop-loss para posiciones cortas en MetaTrader puntos. Establezca en `0` para desactivar la parada de protección. |
| `TradeVolume` | `0.2` | Tamaño de lote utilizado al enviar órdenes de mercado. |
| `MinimumMarginPerLot` | `500` | Requisito de capital aproximado por lote antes de abrir una nueva operación. |
| `CandleType` | Plazo de cinco minutos | Serie de velas que impulsa las actualizaciones de indicadores y la generación de señales. |

## Notas de implementación
- Los indicadores se actualizan manualmente dentro de `ProcessCandle` con precios de apertura de velas para que los valores almacenados siempre representen la barra anterior (reflejando los argumentos `shift=1` utilizados en `iMA`).
- Los niveles de salida se rastrean en campos decimales en lugar de crear colecciones adicionales, lo que cumple con las pautas de alto nivel API.
- Las paradas y los objetivos se evalúan frente a los extremos de las velas; Debido a que solo están disponibles los datos de OHLC, la verificación de parada se realiza antes de la toma de ganancias para emular la prioridad de la orden de MetaTrader lo más fielmente posible.
- La estrategia junta velas con promedios móviles y operaciones propias cuando hay un área del gráfico disponible, lo que simplifica la validación visual.

## Diferencias vs. la versión MetaTrader
- MetaTrader adjunta órdenes de stop-loss y take-profit directamente a los tickets. El puerto StockSharp los reproduce monitoreando los máximos y mínimos de las velas y enviando salidas del mercado; El orden de ejecución intrabar no se puede garantizar si ambos niveles se tocan dentro de la misma barra.
- Los precios de entrada se derivan del cierre de la vela que activó la señal en lugar de la oferta/demanda exacta utilizada por MetaTrader, por lo que el manejo del deslizamiento y el diferencial deben configurarse en el nivel del conector.
- La guardia `AccountFreeMargin()` se aproxima a través de `Portfolio.CurrentValue`. Los usuarios con modelos de margen más detallados pueden ampliar `HasSufficientMargin` si es necesario.
- Se omiten las configuraciones cosméticas MQL como colores, sonidos y deslizamiento; la versión StockSharp se centra en la lógica comercial central.
