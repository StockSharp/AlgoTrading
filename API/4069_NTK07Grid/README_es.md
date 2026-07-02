# NTK_07 Estrategia de cuadrícula
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia NTK_07 es una cuadrícula de órdenes pendientes simétrica escrita originalmente para MetaTrader 4. Coloca un par de órdenes stop alrededor del precio actual y gestiona una pirámide de posiciones estilo martingala utilizando espaciado configurable, stop loss, takeprofit y reglas de seguimiento. El puerto StockSharp mantiene el comportamiento original al tiempo que expone cada configuración como un parámetro de estrategia fuertemente tipado.

La estrategia garantiza continuamente que:

* Un stop de compra y un stop de venta están estacionados en el mercado cuando no hay órdenes activas.
* Después de que se completa una ruptura, la orden pendiente opuesta se cancela para evitar la cobertura.
* Se pueden agregar pedidos adicionales en la misma dirección a `Multiplier` veces el tamaño anterior hasta que se exceda el `LotLimit`.
* Cuando no se permite una mayor escalada, la posición activa está protegida por un trailing stop y, opcionalmente, una toma de ganancias extendida dinámicamente.
* Las órdenes protectoras de parada y toma de ganancias se recrean automáticamente cada vez que cambian los volúmenes o los precios objetivo, de modo que toda la posición abierta siempre comparte los mismos niveles de salida.

## Lógica de trading

1. **Filtro de sesión.** Las operaciones se omiten los sábados y domingos o cuando la hora actual es fuera de `[StartHour, EndHour]`. El rango de horas coincide con la lógica original de MT4: `EndHour = 24` permite operar durante todo el día.
2. **Cheque de capital.** Cuando se adjunta una cartera, el valor de la cuenta actual debe ser al menos `MinCapital` antes de crear cualquier pedido.
3. **Desglose del canal (opcional).** Si `ChannelPeriod` es mayor que cero, se realiza un seguimiento del máximo más alto y del mínimo más bajo de las últimas `ChannelPeriod` velas completadas. Dependiendo de `UseChannelCenter`:
   * `false`: ambas órdenes pendientes se envían solo si el precio de venta está fuera del rango detectado (negociación de ruptura).
   * `true`: las órdenes se envían cuando el precio vuelve al punto medio del rango (estilo de reversión a la media).
4. **Órdenes pendientes iniciales.** Cuando no hay órdenes activas, se coloca un stop de compra `NetStepPips` por encima de la mejor oferta y un stop de venta `NetStepPips` por debajo de la mejor oferta. El volumen base está definido por el módulo de gestión de dinero.
5. **Escalado de posición.** Después de completar una orden, se cancela la orden pendiente opuesta. Si ya hay otra orden activa en la misma dirección, la siguiente orden pendiente se coloca a `NetStepPips` de distancia usando `RoundVolume(previousVolume × Multiplier)`. Cuando el siguiente volumen excede el `LotLimit` calculado, la estrategia deja de agregarse a la cuadrícula.
6. **Detener pérdidas y obtener ganancias.** Cada vez que cambia la posición abierta, la estrategia recrea una parada protectora y (opcionalmente) una orden de toma de ganancias para la exposición larga o corta agregada. Las distancias se derivan de `StopLossPips` y `TakeProfitPips`.
7. **Lógica de equilibrio.** Cuando `UseBreakEven = true` y el precio se mueven `BreakEvenOffsetPips` más allá de la última orden ejecutada, el stop loss se mueve al precio de entrada promedio ponderado por volumen (redondeado usando `PriceRoundingFactor`).
8. **Comportamiento final.** Si no se permite el siguiente paso de escala, la estrategia utiliza el precio de vela más alto/más bajo para mover el stop hacia el mercado en `TrailingStopPips`. Cuando `TrailProfit = true` la distancia de obtención de beneficios también se desplaza, por lo que siempre permanece a `TakeProfitPips` de distancia del último extremo de la vela. Cuando `UseMovingAverageFilter = true` y el precio se negocian contra la media móvil, la distancia de seguimiento se reduce a la mitad, emulando el comportamiento de seguimiento original de medio paso alrededor de una media móvil.

## Gestión monetaria

El puerto admite las tres reglas originales de administración de dinero a través del parámetro `ManagementMode`:

| Modo | Descripción |
| ---- | ----------- |
| `Fixed` | Utilice `InitialLot` para cada pedido nuevo y limite el tamaño por pedido a `LotLimit`. |
| `BalanceBased` | Vuelva a calcular el lote inicial a partir del saldo de la cartera: `ceil(balance / 1000 × PercentRisk / 100)`. El resultado se divide repetidamente por `Multiplier` para proyectar el orden de cuadrícula más pequeño, redondeado por `LotRoundingFactor`. El `LotLimit` original se convierte en el tamaño de lote máximo teórico. |
| `Progressive` | Mantenga `InitialLot` como volumen base pero proyecte el pedido teórico más grande multiplicando por `Multiplier` para cada nivel de cuadrícula. |

Todas las órdenes se redondean usando `LotRoundingFactor` (predeterminado 10 => incrementos de 0,1), mientras que el precio de equilibrio se redondea con `PriceRoundingFactor` (predeterminado 10000 => incrementos de 0,0001).

## Parámetros

| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `NetStepPips` | 23 | Distancia entre niveles de cuadrícula consecutivos. |
| `StopLossPips` | 115 | Distancia de stop-loss aplicada a cada posición. Establezca en 0 para desactivar. |
| `TakeProfitPips` | 300 | Distancia de obtención de beneficios para la posición agregada. Establezca en 0 para desactivar. |
| `TrailingStopPips` | 75 | La distancia de trailing stop se activa cuando ya no es posible escalar. |
| `Multiplier` | 1.7 | Multiplicador de volumen para el siguiente nivel de cuadrícula. |
| `TrailProfit` | `true` | Cuando está habilitado, la toma de ganancias se desplaza junto con el trailing stop. |
| `ManagementMode` | `Progressive` | Regla de administración de dinero seleccionada. |
| `InitialLot` | 1 | Volumen base de pedidos. |
| `LotLimit` | 7 | Tamaño de lote máximo permitido para una única orden pendiente. |
| `MaxTrades` | 4 | Número máximo de niveles de cuadrícula. |
| `PercentRisk` | 10 | Porcentaje de saldo utilizado en la administración del dinero basada en el saldo. |
| `MinCapital` | 5000 | Valor mínimo de cartera requerido antes de negociar. |
| `UseBreakEven` | `false` | Habilite los ajustes de parada de equilibrio. |
| `BreakEvenOffsetPips` | 5 | Umbral de beneficio (en pips) necesario para alcanzar el punto de equilibrio. |
| `UseMovingAverageFilter` | `false` | Habilita la lógica de seguimiento que tiene en cuenta la media móvil. |
| `MovingAverageLength` | 100 | Longitud de la media móvil utilizada en el filtro. |
| `MovingAverageShift` | 0 | Cambio aplicado a la media móvil (los valores de las velas anteriores se utilizan cuando > 0). |
| `StartHour` | 0 | Hora de negociación más temprana permitida (0–23). |
| `EndHour` | 24 | Última hora de negociación permitida (inclusive). |
| `ChannelPeriod` | 0 | Ventana retrospectiva para el filtro central/de ruptura. Establezca en 0 para desactivar el filtro. |
| `UseChannelCenter` | `false` | Cambie entre entradas de estilo de ruptura (`false`) y punto medio (`true`). |
| `LotRoundingFactor` | 10 | Divisor utilizado para redondear volúmenes. |
| `PriceRoundingFactor` | 10000 | Divisor utilizado para redondear el precio de equilibrio. |
| `CandleType` | plazo de 15 minutos | Tipo de vela de trabajo para detección de rango y cálculos de seguimiento. |

## Notas de implementación

* Los libros de pedidos se suscriben para obtener los mejores valores de oferta/demanda precisos antes de realizar pedidos pendientes. Cuando el libro no está disponible, la estrategia vuelve al precio de cierre de la vela.
* Las paradas y objetivos de protección se recrean en lugar de modificarse, porque el API de alto nivel expone ayudas más seguras para registrar pedidos nuevos en lugar de mutar los existentes.
* Los valores de cambio de promedio móvil más allá del historial disponible vuelven al valor más reciente, lo que evita referencias nulas y mantiene el comportamiento cercano a la implementación de MetaTrader.
* Todos los cálculos de precios están normalizados a través de `Security.ShrinkPrice` para que los niveles de parada y límite siempre respeten el tamaño del tick del instrumento.

## Consejos de uso

1. Configure `Strategy.Volume` para definir el multiplicador de tamaño comercial nocional si su corredor requiere escalamiento en relación con el tamaño de la cartera.
2. Al probar símbolos con tamaños de tick exóticos, ajuste `LotRoundingFactor` y `PriceRoundingFactor` en consecuencia para que las operaciones de redondeo sigan siendo significativas.
3. Los parámetros predeterminados se tomaron del EA original para los datos EURUSD H1 entre 2008-01-01 y 2008-11-01. Se recomienda volver a optimizar para otros activos o plazos.
4. Debido a que la cuadrícula puede acumular una gran exposición direccional, supervise siempre los valores `LotLimit` y `MaxTrades` para mantener el riesgo bajo control.
