# Diagrama de estrategia de señales direccionales de dos de tres
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama evalúa tres votos direccionales en cada vela finalizada de 30 minutos: la pendiente de la línea Signal del MACD, la zona de %K de Stochastic y la zona del RSI. Cualquier pareja coincidente produce un único evento mayoritario para esa vela. Cuando el cooldown está listo, una posición en cero se abre con volumen 1, mientras que una posición opuesta se invierte mediante un cierre ReduceOnly seguido de una entrada en la nueva dirección confirmada por la ejecución. Cada ejecución de una nueva posición inicia un cooldown de diez velas.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de 30 minutos alimentan los indicadores MACD 12/26/9, Stochastic 14/3 y RSI 14, configurados para emitir solo cuando están formados. Un bloque Previous value conserva el valor precedente de la línea Signal del MACD, y una compuerta de historial impide tomar una decisión hasta que dicho valor exista.
- Los votos largos son `MACD Signal > Previous MACD Signal`, `Stochastic %K ≤ 20` y `RSI < 40`. Los votos cortos son `MACD Signal < Previous MACD Signal`, `Stochastic %K ≥ 80` y `RSI > 60`. Los valores iguales del MACD y los valores de los osciladores fuera de las zonas direccionales son neutrales.
- Tres bloques de Condición lógica por parejas representan todas las mayorías posibles de dos votos para cada dirección. Un Flag por vela deja pasar únicamente la primera pareja satisfecha, de modo que tres indicadores coincidentes siguen generando un solo evento direccional en lugar de tres.
- Las instantáneas de posición y cooldown encaminan cada mayoría. Si el cooldown está listo y la posición está en cero, se envía una entrada NoCondition a mercado por Order Volume 1. Con una posición opuesta, primero se envía un cierre ReduceOnly a mercado por 1; solo la Order de cierre completamente ejecutada inicia la entrada NoCondition a mercado por 1 en la nueva dirección.
- Un bloque Combination reúne las ejecuciones de las cuatro acciones que abren una nueva posición en un único flujo de cooldown. Una ejecución marca el diagrama como no disponible para entrar, se omiten las diez velas finalizadas siguientes y la undécima vela finalizada es la primera apta para decidir. No hay una salida independiente ni bloques de stop-loss, take-profit o protección de posición.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando coinciden dos votos largos cualesquiera y el cooldown está listo, una posición en cero envía una compra NoCondition a mercado por Order Volume 1. Si la posición es corta, el diagrama envía primero una compra ReduceOnly a mercado por 1; solo su Order de cierre completamente ejecutada activa la compra NoCondition a mercado por 1. Una posición larga existente no se modifica.
- **Entrada en corto**: Cuando coinciden dos votos cortos cualesquiera y el cooldown está listo, una posición en cero envía una venta NoCondition a mercado por Order Volume 1. Si la posición es larga, el diagrama envía primero una venta ReduceOnly a mercado por 1; solo su Order de cierre completamente ejecutada activa la venta NoCondition a mercado por 1. Una posición corta existente no se modifica.
- **Salida**: No existe una regla de salida separada. Una mayoría en la dirección opuesta cierra el lado actual y después abre el nuevo lado mediante la secuencia escalonada. El cierre ReduceOnly no puede aumentar la exposición, pero ambas etapas usan el Order Volume fijo de 1; la secuencia está dimensionada para la posición de una unidad creada por el diagrama, y un tamaño real de posición diferente podría no cerrarse por completo ni terminar en la dirección señalada.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles Series | 00:30:00 | Serie de velas de treinta minutos; solo las velas finalizadas actualizan los indicadores, restablecen los Flag de mayoría por vela, hacen avanzar el cooldown e inician decisiones. |
| MACD Fast Length | 12 | Fijo dentro del bloque Indicator de MACD; edite ese bloque para cambiar el periodo de la EMA rápida. |
| MACD Slow Length | 26 | Fijo dentro del bloque Indicator de MACD; edite ese bloque para cambiar el periodo de la EMA lenta. |
| MACD Signal Length | 9 | Fijo dentro del bloque Indicator de MACD; edite ese bloque para cambiar el periodo de la EMA Signal, cuya pendiente de una vela proporciona el voto del MACD. |
| Stochastic K Length | 14 | Fijo dentro del bloque Indicator de Stochastic; edite ese bloque para cambiar el periodo de %K. Los umbrales largo y corto son 20 y 80. |
| Stochastic D Length | 3 | Fijo dentro del bloque Indicator de Stochastic; edite ese bloque para cambiar el periodo de %D. El voto direccional lee %K y el indicador completo debe estar formado. |
| RSI Length | 14 | Periodo del RSI. Los umbrales direccionales fijos son estrictamente inferiores a 40 y estrictamente superiores a 60. |
| Cooldown Bars | 10 | Número de velas finalizadas posteriores bloqueadas tras la ejecución de una nueva posición; las decisiones se reanudan en la vela 11. |
| Order Volume | 1 | Cantidad fija utilizada por las entradas desde posición cero, las acciones de cierre ReduceOnly y las entradas confirmadas por ejecución después de un cierre. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite únicamente velas finalizadas de 30 minutos. Tres bloques [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html), configurados para emitir solo cuando están formados, calculan MACD 12/26/9, Stochastic 14/3 y RSI 14.
- Un [Conversor](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/converter.html) extrae la línea Signal del MACD. Un bloque [Previous value](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) conserva su valor de una vela atrás, y las comparaciones estrictas clasifican la Signal actual como ascendente, descendente o sin cambios.
- Otros bloques [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) implementan los límites fijos exactos de los osciladores: Stochastic %K utiliza `≤ 20` y `≥ 80`, mientras que RSI utiliza `< 40` y `> 60`. Los umbrales y los dos votos requeridos son ajustes fijos del diagrama, no parámetros expuestos.
- Seis bloques [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) por parejas cubren las tres parejas largas posibles y las tres parejas cortas posibles. Dos bloques [Flag](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/flag.html) se restablecen en cada vela y reducen varias parejas satisfechas a un solo evento mayoritario por dirección.
- La [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual y el estado de cooldown listo se capturan para el ciclo de la vela. Las comparaciones de posición distinguen una exposición en cero, larga o corta, y las condiciones de entrada requieren tanto un evento mayoritario como un cooldown disponible.
- Seis bloques [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) implementan dos entradas desde posición cero y dos inversiones escalonadas. Las rutas desde cero están protegidas externamente por `Position = 0`; los bloques de cierre de inversión usan ReduceOnly, y sus salidas Order completamente ejecutadas activan las entradas NoCondition fijas en la dirección opuesta.
- Un bloque [Combination](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/combination.html) reúne de inmediato las salidas MyTrade de los cuatro bloques de nueva posición, sin contarlas ni modificarlas. La ejecución reunida desactiva la disponibilidad de entrada y activa un bloque [N values](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html), que cuenta diez velas finalizadas posteriores antes de restaurar la disponibilidad para la vela 11. El [Panel de gráfico](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe las velas, tres flujos numéricos de señales, las ejecuciones reunidas de nuevas posiciones y ambas ejecuciones de cierre por inversión.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
