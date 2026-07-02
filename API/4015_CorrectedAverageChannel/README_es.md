# Estrategia de canal promedio corregida
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de canal promedio corregida** es una adaptación de C# del asesor experto MetaTrader `e-CA-5`. El sistema reconstruye el indicador "Promedio Corregido" (CA) cada vez que se cierra una vela y abre una posición cuando el precio cruza el promedio móvil corregido por un desplazamiento sigma configurable. La implementación convertida se basa en la vela de alto nivel API de StockSharp, utiliza órdenes de mercado y gestiona salidas protectoras (stop-loss, take-profit, trailing stop) internamente para reflejar el comportamiento del Asesor Experto original.

## Indicador de promedio corregido
El filtro CA combina una media móvil con retroalimentación de volatilidad. La versión MQL expone tres entradas: longitud del promedio móvil, método de promedio y precio aplicado. En el puerto StockSharp:

1. El tipo de media móvil se selecciona mediante el parámetro `MaTypeOption` (SMA, EMA, SMMA, LWMA) y la longitud `MaPeriod`.
2. Un indicador `StandardDeviation` con el mismo período mide la volatilidad actual.
3. Para cada vela terminada, el valor corregido se calcula de forma iterativa:
   - Sea `M_t` el valor MA en la última barra y `CA_{t-1}` el valor corregido de la barra anterior.
   - Calcule `v1 = StdDev_t^2` y `v2 = (CA_{t-1} - M_t)^2`.
   - Si es `v2 <= 0` o `v2 < v1`, mantenga el factor de corrección `k = 0`. De lo contrario, configure `k = 1 - v1 / v2`.
   - Actualización `CA_t = CA_{t-1} + k * (M_t - CA_{t-1})`.
   - El primer valor corregido por defecto es la propia media móvil.

Este circuito de retroalimentación amortigua la MA durante los períodos de calma y permite ajustes rápidos cuando el precio diverge más allá de la estimación de volatilidad actual.

## Lógica comercial
1. La estrategia se suscribe al tipo de vela configurado (`CandleType`) y espera hasta que tanto la media móvil como la desviación estándar estén completamente formadas.
2. Una vez que termina una vela, el algoritmo calcula el nuevo valor corregido y compara el cierre de la vela anterior con el nivel corregido anterior.
3. Dos compensaciones sigma, `SigmaBuyPoints` y `SigmaSellPoints`, se convierten en distancias de precios utilizando el `PriceStep` del instrumento.
4. Las reglas de entrada utilizan el cierre de la vela anterior y el nivel corregido recién calculado:
   - **Comprar** si el cierre anterior estuvo por debajo del promedio corregido más la sigma de compra, y el cierre actual termina por encima de ese límite superior.
   - **Vender** si el cierre anterior estuvo por encima del promedio corregido menos el sigma de venta y el cierre actual termina por debajo de ese límite inferior.
5. Sólo se permite una posición neta. Se envía una nueva operación solo cuando no hay exposición presente.

Debido a que la versión StockSharp opera en velas terminadas, la confirmación de ruptura ocurre una vez por barra en lugar de cada tick, lo que proporciona un comportamiento determinista adecuado para pruebas retrospectivas y automatización en vivo con datos de velas.

## Gestión de riesgos
El puerto reproduce los tres mecanismos de protección del Asesor Experto original:

- **Stop-loss fijo**: `StopLossPoints` multiplicado por el paso del precio define la distancia entre el precio de entrada y el stop de protección. Un stop activado cierra toda la posición con una orden de mercado.
- **Obtención de ganancias fija**: `TakeProfitPoints` se convierte en una distancia objetivo de ganancias. Cuando el precio alcanza el nivel durante una vela, la posición se cierra con una orden de mercado.
- **Tope dinámico**: cuando `TrailingPoints` es mayor que cero, la estrategia rastrea las ganancias no realizadas y, una vez que el precio ha avanzado al menos esa distancia, almacena un nivel dinámico detrás del último cierre. El trailing stop solo avanza y respeta `TrailingStepPoints`, lo que representa la mejora mínima antes de que se acepte un nuevo nivel de seguimiento. Los niveles finales se redondean con `Security.ShrinkPrice` para que se alineen con el tamaño de tick del instrumento.

Todas las salidas restablecen el estado de riesgo interno. Cuando aparece la siguiente señal, los niveles de parada, objetivo y seguimiento se recalculan a partir del nuevo precio de ejecución, lo que garantiza un comportamiento cercano a la versión MQL que modifica las protecciones de la orden original.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `OrderVolume` | Cantidad utilizada para las entradas al mercado. Debe ser positivo. |
| `TakeProfitPoints` | Objetivo de beneficio en pasos de precio (0 desactiva la toma de beneficios). |
| `StopLossPoints` | Distancia del stop-loss en pasos de precio (0 desactiva el stop-loss). |
| `TrailingPoints` | Distancia de beneficio (en pasos de precio) requerida antes de que se active el trailing stop. |
| `TrailingStepPoints` | Distancia mínima extra que se debe capturar antes de volver a mover el trailing stop. |
| `MaPeriod` | Período tanto de la media móvil como de la desviación estándar. |
| `MaTypeOption` | Tipo de media móvil: SMA, EMA, SMMA o LWMA. |
| `SigmaBuyPoints` | La compensación de Sigma se agregó por encima del promedio corregido antes de abrir una posición larga. |
| `SigmaSellPoints` | El desplazamiento de Sigma se restó por debajo del promedio corregido antes de abrir una posición corta. |
| `CandleType` | Serie de velas utilizadas para cálculos de indicadores y evaluación de señales. |

Todos los parámetros numéricos admiten la optimización a través de `SetCanOptimize(true)`, por lo que la estrategia se puede calibrar directamente dentro del entorno StockSharp.

## Notas de uso
- El tipo de vela predeterminado es de una hora. Ajústelo para que coincida con el período de tiempo que se utilizó al optimizar la estrategia MetaTrader original.
- `Security.PriceStep` se utiliza para traducir todas las entradas de "puntos" a distancias de precios reales. Los instrumentos sin un paso configurado vuelven a `1`, preservando el comportamiento sensato para índices o criptomonedas.
- La estrategia se ejecuta sólo en velas terminadas. Si se requiere precisión intrabarra, reduzca el período de tiempo a la granularidad deseada.
- Los trailingstops se implementan con órdenes de mercado cuando se violan, imitando el EA original que modificó los precios de stop-loss. Este enfoque evita colocar órdenes stop adicionales y mantiene la gestión de riesgos contenida dentro de la propia estrategia.
- No se proporciona ninguna versión de Python para esta conversión, según los requisitos de la tarea.

## Diferencias con el EA original
- El API basado en velas de StockSharp reemplaza el procesamiento a nivel de tick; Todas las decisiones se toman cuando se cierra una vela.
- La gestión de órdenes se compensa: las posiciones opuestas no se mantienen simultáneamente, lo que coincide con la lógica de orden única de la versión MetaTrader.
- Las paradas protectoras y las salidas dinámicas se ejecutan mediante órdenes de mercado en lugar de modificar los tickets de órdenes existentes. Este comportamiento es equivalente en la compensación de cuentas y al mismo tiempo mantiene la implementación coherente con otras estrategias StockSharp.

Estas adaptaciones preservan la idea comercial de `e-CA-5` al tiempo que alinean la lógica con las mejores prácticas de StockSharp y las convenciones de alto nivel API descritas en las pautas del repositorio.
