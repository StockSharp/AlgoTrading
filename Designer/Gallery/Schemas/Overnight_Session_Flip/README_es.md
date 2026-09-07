# Diagrama de la estrategia Overnight Session Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama combina una SMA formada de 20 periodos con dos franjas programadas para órdenes a mercado. El reloj de la estrategia evalúa la última vela finalizada de cinco minutos, la posición y la fecha del calendario: una compra válida puede enviarse durante la hora 20 y una venta válida durante la hora 8. Un registro de fecha limita el diagrama a una orden enviada por fecha del calendario.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de cinco minutos actualizan el precio de cierre y SimpleMovingAverage con periodo 20.
- El bloque SMA solo emite valores formados, por lo que las decisiones programadas esperan hasta que el indicador tenga suficiente historial de velas.
- El bloque Time actúa como reloj de decisión. Cada pulso libera los últimos valores guardados de cierre, SMA y posición, y después entrega la hora y los componentes del calendario usados por los filtros.
- Los dos bloques de órdenes a mercado usan un volumen fijo de 1 sin condición de modificación de posición. Los filtros solo permiten comprar con posición menor o igual que cero y vender con posición mayor o igual que cero.
- Una clave numérica de fecha se registra antes de activar el bloque de orden. El gráfico muestra velas, valores SMA y los dos flujos MyTrade; el diagrama no incluye protección ni un bloque de salida separado.

## Reglas de entrada y salida

- **Entrada en largo**: Durante Night Hour 20, el cierre de la última vela finalizada está por encima de la SMA formada, la posición actual es menor o igual que cero y todavía no se ha enviado una orden en la fecha actual. El diagrama envía una compra a mercado por Volume 1.
- **Entrada en corto**: Durante Day Hour 8, el cierre de la última vela finalizada está por debajo de la SMA formada, la posición actual es mayor o igual que cero y todavía no se ha enviado una orden en la fecha actual. El diagrama envía una venta a mercado por Volume 1.
- **Salida**: No hay una orden de salida dedicada ni órdenes de protección. Una orden posterior de tamaño fijo en la dirección opuesta puede reducir una posición, cerrar una posición contraria del mismo tamaño o cruzar cero cuando la magnitud actual es menor que Volume; no garantiza una reversión completa.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:05:00 | Marco temporal de cinco minutos; solo las velas finalizadas actualizan los valores guardados de precio y SMA. |
| SMA Period | 20 | Número de velas finalizadas usado por SimpleMovingAverage; las decisiones requieren una SMA formada. |
| Night Hour | 20 | Hora del reloj de la estrategia en la que las condiciones de compra pueden enviar una orden. |
| Day Hour | 8 | Hora del reloj de la estrategia en la que las condiciones de venta pueden enviar una orden. |
| Volume | 1 | Cantidad fija suministrada a los dos bloques de órdenes a mercado. |

## Detalles del diagrama

- El flujo de [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) alimenta un [Conversor](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/converters/converter.html) del precio de cierre y un [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) que solo entrega la SMA formada. Una [Fórmula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) con la expresión `a` expone la SMA como valor numérico.
- [Hora actual](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/time/current_time.html) proporciona la marca temporal de la estrategia o del mensaje. En cada pulso activa bloques de [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) que conservan los últimos valores de cierre, SMA y posición, por lo que Time participa directamente en cada decisión.
- Los conversores temporales extraen Hour, Year y DayOfYear. La [Fórmula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) de fecha calcula `Year * 1000 + DayOfYear` y produce una clave estable para cada fecha del calendario.
- Los bloques de [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) evalúan las dos horas programadas, el cierre frente a la SMA, la posición frente a cero y la clave de fecha actual frente a la última clave registrada. Dos bloques de [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) combinan los filtros de compra y venta.
- La [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual se muestrea en cada pulso del reloj. La compra requiere `Position <= 0` y la venta requiere `Position >= 0`.
- Una señal combinada verdadera primero registra la clave de la fecha actual y después activa su bloque [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html). Este orden impide enviar otra orden en pulsos posteriores de la misma fecha del calendario.
- Los dos bloques Modify position reciben el valor fijo Volume compartido y colocan órdenes a mercado. El panel Chart recibe velas finalizadas, el flujo de SMA formada y las salidas MyTrade de los bloques de compra y venta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
