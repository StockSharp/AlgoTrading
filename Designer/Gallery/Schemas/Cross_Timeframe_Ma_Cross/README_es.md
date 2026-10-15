# Diagrama de estrategia de cruce de medias móviles entre marcos temporales
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama combina una media móvil simple de 10 períodos sobre velas finalizadas de cuatro horas con una media móvil simple de 40 períodos sobre velas finalizadas de una hora. Ambas configuraciones representan una ventana nominal de 40 horas. El último valor formado de la media del marco superior se conserva y se empareja con la media base actual una vez por cada vela base finalizada; la dirección del cruce y la posición actual encaminan acciones de mercado de 0.1 fijo, incluidas reversiones en dos etapas confirmadas por ejecución. El gráfico muestra velas de una hora, las dos medias sincronizadas y cuatro flujos de ejecuciones.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de cuatro horas alimentan Higher SMA 10, mientras que las velas finalizadas de una hora alimentan Base SMA 40 y activan el ciclo de decisión. Con la configuración predeterminada, cada media cubre 40 horas nominales: 10 × 4 horas y 40 × 1 hora.
- Se conserva el último valor formado de Higher SMA. En cada vela de una hora finalizada, el diagrama actualiza ese valor conservado y la Base SMA actual; después, un bloque Sync con intervalo de una hora y la vela base como ancla libera juntos los valores alineados.
- Un solo bloque Crossing emite `true` cuando Higher SMA cruza por encima de Base SMA y `false` cuando cruza por debajo. Un bloque NOT convierte el evento bajista en un disparador positivo para la ruta corta.
- La posición actual separa cada cruce en los casos sin posición, posición larga y posición corta. Sin posición se abre 0.1 en la dirección de la señal, una posición ya alineada no cambia y una posición contraria inicia una reversión por etapas.
- La reversión por etapas envía primero una acción de mercado ReduceOnly por 0.1. Solo la Order de cierre totalmente ejecutada activa una acción de mercado NoCondition fija por 0.1 en la nueva dirección. La secuencia está dimensionada para una posición creada por el diagrama con el mismo Order Volume; un tamaño real diferente puede no terminar en la exposición objetivo. No hay bloques de stop-loss ni take-profit.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando Crossing emite un evento alcista, la rama filtrada de posición nula envía una compra a mercado NoCondition por Order Volume 0.1. Una posición corta envía primero una compra a mercado ReduceOnly por 0.1; solo su Order totalmente ejecutada activa la segunda compra NoCondition por 0.1. Una posición larga existente no cambia.
- **Entrada en corto**: Cuando Crossing emite un evento bajista, NOT activa la ruta corta. La rama filtrada de posición nula envía una venta a mercado NoCondition por Order Volume 0.1. Una posición larga envía primero una venta a mercado ReduceOnly por 0.1; solo su Order totalmente ejecutada activa la segunda venta NoCondition por 0.1. Una posición corta existente no cambia.
- **Salida**: No existe una regla independiente de salida, stop-loss o take-profit. Un cruce válido en la dirección contraria ejecuta la secuencia de cierre y apertura con cantidad fija. Su primer paso ReduceOnly no puede aumentar ni invertir la exposición, pero la acción NoCondition posterior no se redimensiona para una posición externa; si el tamaño real difiere de Order Volume, la posición final objetivo no está garantizada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Higher Candles Series | 04:00:00 | Serie de velas de cuatro horas utilizada por Higher SMA. Solo las velas finalizadas actualizan la media conservada del marco superior. |
| Base Candles Series | 01:00:00 | Serie de velas de una hora utilizada por Base SMA. Cada vela finalizada ancla una evaluación sincronizada y también se dibuja en el gráfico. |
| Higher SMA Length | 10 | Período de la media móvil simple calculada sobre velas de cuatro horas. Diez velas representan una ventana nominal de 40 horas. |
| Higher SMA Source | unset | Se deja sin configurar, por lo que Higher SMA lee el precio Close de cada vela finalizada de cuatro horas. |
| Base SMA Length | 40 | Período de la media móvil simple calculada sobre velas de una hora. Cuarenta velas representan la misma ventana nominal de 40 horas. |
| Base SMA Source | unset | Se deja sin configurar, por lo que Base SMA lee el precio Close de cada vela finalizada de una hora. |
| Order Volume | 0.1 | Cantidad fija utilizada para entradas sin posición, cierres ReduceOnly y la segunda etapa de una reversión confirmada por ejecución. |

## Detalles del diagrama

- Dos bloques [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emiten únicamente velas finalizadas de cuatro horas y una hora. Dos bloques [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) independientes y limitados a valores formados calculan SimpleMovingAverage 10 y SimpleMovingAverage 40 sobre sus precios Close.
- Un bloque [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) conserva el último valor formado de Higher SMA. Cada vela base finalizada actualiza ese valor y Base SMA antes de entrar en [Sync](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/sync.html), cuyo Interval es `01:00:00`, ClearSockets está activado y la entrada de vela proporciona el ancla horaria.
- Las salidas numéricas sincronizadas entran en un único bloque [Cruce](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/crossing.html). Su evento alcista `true` conduce a la ruta larga, mientras que una [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) NOT convierte el evento bajista `false` en un disparador corto positivo.
- La [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual se actualiza en el mismo ciclo de la vela base. Los bloques de [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) distinguen `Position = 0`, `Position > 0` y `Position < 0`, por lo que una posición ya alineada no puede recibir otra entrada.
- Para un evento alcista, la ruta sin posición filtrada externamente invoca un bloque de compra [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) con NoCondition. La ruta corta invoca primero una compra ReduceOnly; su salida Order solo aparece después de la ejecución completa del cierre y entonces prepara la compra NoCondition fija.
- La ruta bajista es simétrica: la ruta sin posición filtrada externamente abre un corto con NoCondition, mientras que una posición larga se reduce mediante una venta antes de que su Order totalmente ejecutada prepare la venta NoCondition fija. Las cuatro acciones usan MarketOrder y Order Volume 0.1. No hay bloques de stop-loss, take-profit ni salida temporizada.
- El [Panel de gráfico](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe las velas finalizadas de una hora, los valores sincronizados de Higher SMA y Base SMA, y las salidas MyTrade de las acciones de apertura larga, apertura corta, cierre de corto y cierre de largo.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
