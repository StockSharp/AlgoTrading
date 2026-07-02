# Estrategia MTrendLine
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia MTrendLine** transfiere el script MetaTrader `MTrendLine.mq4` a la estrategia de alto nivel de StockSharp API. el original
El asesor experto ajusta repetidamente el precio de las órdenes pendientes existentes para que permanezcan alineadas con una línea de tendencia dibujada en el
gráfico. La versión StockSharp automatiza el mismo comportamiento reconstruyendo la línea de tendencia móvil con un configurable
Indicador `LinearRegression`. Hasta tres espacios de órdenes pendientes independientes pueden seguir la línea de regresión calculada con sus
propio tipo de orden, distancia y volumen. Cada vez que se cierra una nueva vela, la estrategia vuelve a calcular el valor de la línea, evalúa el
compensaciones requeridas y actualiza las órdenes pendientes en consecuencia.

El puerto agrega mejoras modernas de riesgo y usabilidad, como parámetros estructurados y conversión automática desde MetaTrader puntos.
en pasos de precios reales y distancias opcionales de stop-loss/take-profit que se mueven junto con las órdenes pendientes. Oferta/Pregunta
Las actualizaciones se monitorean a través de `SubscribeLevel1()`, por lo que la estrategia respeta la distancia mínima que los corredores exigen entre los
precio de mercado actual y órdenes en reposo.

## Lógica comercial
1. Suscríbase a la serie de velas configuradas a través de `SubscribeCandles()` y alimente un indicador `LinearRegression` con cada una
finished bar. El indicador representa la línea de tendencia manual de la versión MetaTrader.
2. Mantenga las suscripciones de Nivel 1 para almacenar en caché los últimos valores de mejor oferta y mejor demanda. Se utilizan para hacer cumplir el mínimo.
parámetro de distancia antes de reubicar una orden pendiente.
3. Para cada espacio habilitado, calcule el precio deseado como **valor de regresión + distancia × tamaño de punto**. El tamaño en puntos por defecto es
el paso del precio del valor, pero se puede anular para que coincida con la constante `Point` de MetaTrader.
4. Convierta la configuración de ranura en StockSharp ayudantes de pedidos (`BuyLimit`, `SellLimit`, `BuyStop`, `SellStop`). Opcional
Los precios de stop-loss y take-profit se derivan de la distancia solicitada en puntos para que realicen un seguimiento de la orden después de cada movimiento.
5. Si ya existe una orden pendiente activa para el espacio y el nuevo precio objetivo difiere, cancele la orden actual primero y
espera la siguiente vela para colocar la actualizada. Esto refleja el comportamiento de `OrderModify` del código MQL sin
arriesgando solicitudes duplicadas.
6. Cuando un espacio está deshabilitado o el precio calculado deja de ser válido (por ejemplo, negativo), cancele la orden pendiente asociada.
and clear its cached state.

## Espacios para pedidos pendientes
Cada ranura emula una llamada a `modify()` en el EA original. Las ranuras se pueden configurar de forma independiente:
- **Tipo**: elija entre límite de compra, límite de compra, límite de venta o límite de venta.
- **Distancia** — distancia en MetaTrader puntos sumados al valor de regresión para obtener el nuevo precio. Utilice valores negativos para
posicionar órdenes por debajo de la línea de regresión.
- **Volume** — size of the pending order. Si se establece en cero o negativo, la estrategia vuelve al `TradeVolume` global.
- **Activar indicador**: permite desactivar una ranura sin eliminar su configuración. Las ranuras deshabilitadas cancelan automáticamente cualquier activa
órdenes que les pertenecen.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | velas de 1 hora | Marco temporal principal utilizado para construir la línea de tendencia de regresión. |
| `RegressionLength` | `int` | `24` | Número de velas completadas introducidas en el indicador `LinearRegression`. |
| `PointValue` | `decimal` | `0` | Valor monetario de un MetaTrader punto. Cuando es cero, la estrategia utiliza el paso del precio del valor. |
| `TradeVolume` | `decimal` | `1` | Volumen predeterminado utilizado por todas las ranuras cuando su propio volumen es cero. |
| `StopLossPoints` | `decimal` | `0` | Distancia de stop-loss en puntos. Establezca en cero para deshabilitar la colocación automática de stop-loss. |
| `TakeProfitPoints` | `decimal` | `0` | Distancia de toma de ganancias en puntos. Establezca en cero para deshabilitar la colocación automática de ganancias. |
| `MinDistancePoints` | `decimal` | `0` | Diferencia mínima (en puntos) que debe existir entre la mejor oferta/demanda y la orden pendiente. |
| `PendingOrder{1,2,3}Enabled` | `bool` | Ranura específica | Habilita o deshabilita la ranura dada. |
| `PendingOrder{1,2,3}Mode` | `enum` | Ranura específica | Tipo de orden pendiente: BuyLimit, BuyStop, SellLimit o SellStop. |
| `PendingOrder{1,2,3}DistancePoints` | `decimal` | Ranura específica | Distancia (en puntos) agregada al valor de regresión para calcular el precio del pedido. |
| `PendingOrder{1,2,3}Volume` | `decimal` | Ranura específica | Volumen para la ranura. Zero reuses `TradeVolume`. |

## Diferencias frente al script MetaTrader original
- MetaTrader modifica los pedidos existentes. StockSharp utiliza la semántica de cancelar y reemplazar mientras espera la confirmación
antes de registrar la orden de reemplazo en la siguiente vela.
- El código original lee el valor de una línea de tendencia dibujada manualmente. El puerto reemplaza esto con un automático
`LinearRegression` indicador para que el comportamiento sea determinista y pueda ejecutarse sin supervisión.
- `MODE_STOPLEVEL` no está disponible en StockSharp. En cambio, la estrategia proporciona el `MinDistancePoints` configurable
parámetro y lo aplica mediante actualizaciones de oferta/demanda en tiempo real.
- Las distancias de stop-loss y take-profit son parámetros opcionales en lugar de leer la configuración de la orden existente. Esto mantiene los valores.
consistent across order re-registrations.

## Consejos de uso
- Establezca `PointValue` para que coincida con la definición de puntos del corredor si difiere del valor `PriceStep`; esto garantiza la
Los parámetros de distancia reflejan sus homólogos MetaTrader.
- Habilite solo las ranuras que necesite. Cada espacio mantiene su propio pedido pendiente y comentario (`"MTrendLine slot N"`) para identificar
incluirlos en informes o en el Registro de pedidos es sencillo.
- Considere combinar la estrategia con los asistentes de protección contra riesgos integrados de StockSharp si necesita paradas dinámicas o cuentas.
controles de nivel. La implementación se centra en reflejar la lógica original de modificación de pedidos.

## Indicadores
- `LinearRegression` aplicado a velas terminadas.
