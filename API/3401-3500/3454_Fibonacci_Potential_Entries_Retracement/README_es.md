# Fibonacci Estrategia de retroceso de entradas potenciales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Fibonacci estrategia de retroceso de entradas potenciales** recrea el MetaTrader experto `EA_PUB_FibonacciPotentialEntries`. El algoritmo espera cotizaciones activas de Nivel 1 y luego coloca dos órdenes pendientes alrededor de los niveles de retroceso Fibonacci proporcionados manualmente. Cuando se alcanza el objetivo de beneficio compartido, la estrategia reduce cada posición en un 50% y mueve el tope de protección al punto de equilibrio para la cantidad restante.

## Mapeo de la lógica original.
- **Órdenes de entrada**: se emiten dos órdenes límite una vez que los precios de mejor oferta y mejor oferta están disponibles:
  - *Primer pedido*: realizado al 50% de retroceso (`P50Level`). El stop-loss está anclado tres diferenciales por debajo (modo alcista) o por encima (modo bajista) del nivel del 61%.
  - *Segunda orden*: colocada en el retroceso del 61% (`P61Level`) con el stop-loss definido a tres diferenciales del punto medio entre los niveles del 61% y el 100%.
- **Sesgo de dirección**: la entrada `bType` original se convierte en el parámetro `MarketBias` (`Bull` para límites de compra, `Bear` para límites de venta).
- **Asignación de riesgos**: la primera operación siempre arriesga `0.7%` del capital de la cartera. La segunda operación consume la porción restante de `RiskPercent` (`max(RiskPercent - 0.7, 0)`), manteniendo la división utilizada por EA.
- **Cálculo del volumen**: el riesgo se traduce al tamaño de la posición hasta `Portfolio.CurrentValue` (con alternativas a `CurrentBalance` y `BeginValue`) junto con el paso de precio, el costo del paso y el multiplicador del instrumento.
- **Obtención de ganancias parcial**: cuando el precio cruza `TargetLevel`, cada operación ejecutada envía una orden de mercado para cerrar la mitad de su volumen abierto. Luego, la orden de parada se mueve al precio de entrada registrado, coincidiendo con la secuencia `OrderClose` + `OrderModify` de EA.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `P50Level` | Precio asignado al 50% de retroceso Fibonacci. |
| `P61Level` | Precio asignado al retroceso del 61,8% Fibonacci. |
| `P100Level` | Precio asignado al retroceso del 100% Fibonacci (utilizado para el punto medio de parada). |
| `TargetLevel` | Objetivo de ganancias compartido para ambas operaciones. |
| `RiskPercent` | Presupuesto de riesgo total en porcentaje del patrimonio (debe ser ≥ 0,7). |
| `MarketBias` | Elige una campaña larga (`Bull`) o corta (`Bear`). |

## Detalles de ejecución
1. Suscríbase a cotizaciones de nivel 1 a través de `SubscribeLevel1()` y espere valores de oferta y demanda positivos.
2. Calcule el diferencial, los niveles de parada y el tamaño de las posiciones. Los pedidos se envían una vez por ejecución y no se volverán a crear automáticamente después (el mismo comportamiento que el experto MQL).
3. Al ejecutarse, la estrategia registra el precio de entrada promedio, coloca la orden de parada adecuada y rastrea el volumen abierto por tramo.
4. Cuando el mercado imprime más allá de `TargetLevel`, la estrategia envía una orden de mercado de cierre parcial por tramo y posteriormente mueve el tope al punto de equilibrio para la cantidad restante.
5. Las órdenes stop se cancelan cuando no queda volumen o cuando la estrategia se detiene.

## Notas y limitaciones
- El stop-loss se regenera cada vez que cambia el tamaño de la posición. Si el corredor rechaza las órdenes de suspensión, verifique los permisos del conector y ajuste la configuración específica del intercambio en consecuencia.
- La toma de ganancias no se registra como orden pendiente. En cambio, el algoritmo refleja el EA al monitorear el nivel de precios y gestionar las salidas en tiempo real.
- Debido a que los pedidos se crean solo una vez, reinicie la estrategia para actualizar los pedidos pendientes después de que cambien los parámetros (idéntico al flujo de trabajo MetaTrader.
