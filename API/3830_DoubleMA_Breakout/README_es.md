# Estrategia de ruptura de doble MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de ruptura de Double MA** es una StockSharp adaptación del MetaTrader asesor experto `DoubleMA_Breakout`. La estrategia monitorea un promedio móvil rápido y lento en velas terminadas. Cuando el promedio rápido se mueve por encima del lento, se coloca una orden stop de compra a una distancia de ruptura configurable por encima del último cierre. Cuando el promedio rápido cae por debajo del lento, se coloca una parada de venta simétricamente por debajo del mercado. Las órdenes pendientes se cancelan y las posiciones abiertas se aplanan cuando el cruce se invierte o se cierra la ventana de negociación.

La conversión mantiene la lógica de ruptura central, agrega administración de pedidos de alto nivel y expone una configuración extensa a través de parámetros `StrategyParam<T>`. Todos los comentarios del código se reescribieron en inglés según lo solicitado.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `FastMaPeriod` | 2 | Período de la media móvil rápida. |
| `SlowMaPeriod` | 5 | Período de la media móvil lenta. |
| `FastMaMode` | `Simple` | Tipo de media móvil para la línea rápida (SMA, EMA, SMMA, LWMA, LSMA). |
| `SlowMaMode` | `Simple` | Tipo de media móvil para la línea lenta. |
| `FastAppliedPrice` | `Close` | Precio aplicado para el promedio rápido (cierre, apertura, máximo, mínimo, mediana, típico, ponderado). |
| `SlowAppliedPrice` | `Close` | Precio aplicado para el promedio lento. |
| `SignalShift` | 1 | Número de velas completadas para mirar hacia atrás al evaluar el cruce. `0` significa la vela actual. |
| `BreakoutDistancePoints` | 45 | Distancia de ruptura en los pasos de precios utilizada para colocar órdenes stop lejos del último cierre. |
| `UseTimeWindow` | `true` | Habilita el filtro de horas de inicio/parada. |
| `StartHour` | 11 | Primera hora (inclusive) en la que se permiten nuevas operaciones. |
| `StopHour` | 16 | Última hora (inclusive) en la que se permite operar. |
| `UseFridayCloseAll` | `true` | Cierre posiciones y cancele todas las órdenes pendientes una vez que se llegue a la hora de cierre del viernes. |
| `FridayCloseTime` | 21:30 | Hora del día del viernes en la que la estrategia realiza un piso duro. |
| `UseFridayStopTrading` | `false` | Deshabilite las nuevas entradas después de la hora de parada configurada del viernes manteniendo las posiciones existentes. |
| `FridayStopTradingTime` | 19:00 | Hora del día del viernes en la que se bloquean nuevas entradas (si está habilitado). |
| `CandleType` | 1 hora | Tipo de datos de vela utilizado tanto para indicadores como para señales. |

## Lógica de trading
1. Suscríbase a las velas terminadas definidas por `CandleType` y calcule dos promedios móviles según los modos seleccionados y los precios aplicados.
2. Mantenga historiales cortos de valores de indicadores para que la estrategia pueda hacer referencia a la vela seleccionada por `SignalShift` sin violar la directriz "no GetValue".
3. **Configuración alcista:** cuando el MA rápido está por encima del MA lento en la vela de señal, cancele cualquier stop de venta, cierre posiciones cortas y coloque una orden stop de compra `BreakoutDistancePoints × PriceStep` por encima del último cierre si no quedan órdenes ni posiciones.
4. **Configuración bajista:** cuando la MA rápida está por debajo de la MA lenta en la vela de señal, cancele cualquier parada de compra, cierre posiciones largas y coloque una orden de parada de venta a la misma distancia por debajo del mercado.
5. **Gestión del tiempo:** si la ventana de negociación está deshabilitada o cerrada, todas las órdenes pendientes se cancelan. Los viernes, antes del fin de semana se respetan los tiempos opcionales de stop-trading y hard-flat.
6. Cuando se ejecuta una orden stop, la orden pendiente opuesta se cancela para evitar múltiples operaciones simultáneas.

## Diferencias con el MetaTrader EA
- Los conmutadores de administración de dinero y los esquemas de trailing-stop personalizados del script original no se trasladan. La propiedad `Volume` de StockSharp define el tamaño de la operación y se puede agregar control de riesgos a través de módulos de protección estándar.
- Los reintentos de error y los bucles de pedidos de bajo nivel se reemplazan con ayudantes StockSharp de alto nivel (`BuyStop`, `SellStop`, `ClosePosition`, `CancelOrder`).
- Se omiten conceptos específicos de los corredores, como cortes de márgenes o correcciones de deslizamiento; Estos se pueden implementar por separado si es necesario.
- El modo LSMA utiliza el indicador `LinearRegression` de StockSharp para aproximar el promedio móvil de mínimos cuadrados utilizado en MetaTrader.

## Notas de uso
- Configure `Volume` antes de iniciar la estrategia; de forma predeterminada, StockSharp utiliza un único lote/contrato.
- Combine la estrategia con `StartProtection` (ya invocado en el código) para adjuntar módulos de stop-loss o take-profit a nivel de plataforma si es necesario.
- Para optimizar los flujos de trabajo, habilite los parámetros deseados a través de la configuración `.SetCanOptimize` proporcionada en el constructor.
- Asegúrese de que el instrumento tenga un `PriceStep` válido; de lo contrario, la distancia de ruptura vuelve a ser `1` para evitar compensaciones de cero.
