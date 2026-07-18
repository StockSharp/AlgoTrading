# Estrategia Forex Fraus M1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Forex Fraus M1 replica el asesor experto MetaTrader 5 "Forex Fraus M1" en el framework StockSharp. Es un sistema contrario que monitorea un oscilador Williams %R de largo período (período 360) en velas de un minuto. Cada vez que el oscilador toca valores extremos, la estrategia intenta desvanecerse del movimiento, apuntando a una rápida reversión hacia el punto medio del rango reciente. La implementación mantiene la gestión de dinero del experto original, incluyendo horas de trading opcionales, niveles estáticos de stop-loss y take-profit medidos en pips y un trailing stop basado en pips.

## Lógica de trading
- **Indicador**: Williams %R con un período de 360.
- **Señal de compra**: Cuando Williams %R cae por debajo de `-99.9`, el mercado se considera extremadamente sobrevendido. La estrategia envía una orden de compra al mercado si no hay posición larga existente. Si `CloseOppositePositions` está habilitado, cualquier exposición corta se cierra en la misma solicitud de orden.
- **Señal de venta**: Cuando Williams %R sube por encima de `-0.1`, el mercado está extremadamente sobrecomprado. La estrategia emite una orden de venta al mercado, opcionalmente cerrando primero cualquier exposición larga abierta.
- **Filtro de tiempo**: Cuando `UseTimeControl` está habilitado, la estrategia solo evalúa señales entre `StartHour` (inclusive) y `EndHour` (exclusive). Si la sesión cruza la medianoche (`StartHour > EndHour`), el trading se permite de `StartHour` a 23 y de 0 a `EndHour - 1`.

## Gestión de riesgo
- **Stop-loss**: Calculado como `StopLossPips * PipSize` por debajo (para largos) o por encima (para cortos) del precio de entrada. Cuando el mínimo de la vela toca el nivel de stop, la posición se cierra al mercado.
- **Take-profit**: Calculado como `TakeProfitPips * PipSize` por encima (para largos) o por debajo (para cortos) del precio de entrada. Cuando el máximo/mínimo de la vela alcanza este nivel, la posición se cierra para asegurar ganancias.
- **Trailing stop**: Si tanto `TrailingStopPips` como `TrailingStepPips` son positivos, el stop se aprieta una vez que el precio se mueve al menos `TrailingStopPips + TrailingStepPips` pips a favor de la operación. Para largos el stop sigue el cierre menos `TrailingStopPips`; para cortos sigue el cierre más `TrailingStopPips`.
- **Tamaño de pip**: `PipSize` define el valor monetario de un pip. Para símbolos Forex de cinco dígitos establezca `PipSize` en `0.0001`, para pares JPY de tres dígitos use `0.01`, etc.

La estrategia verifica las condiciones de stop-loss y take-profit usando los máximos/mínimos de las velas. Cuando ambos se tocan dentro de la misma vela, el stop de protección tiene prioridad, reflejando el comportamiento conservador del experto original.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Volumen de operación usado para nuevas posiciones. |
| `StopLossPips` | `50` | Distancia del stop-loss en pips desde el precio de entrada. Establezca en cero para deshabilitar. |
| `TakeProfitPips` | `150` | Distancia del take-profit en pips desde el precio de entrada. Establezca en cero para deshabilitar. |
| `TrailingStopPips` | `1` | Distancia base del trailing stop en pips. Establezca en cero para deshabilitar el seguimiento. |
| `TrailingStepPips` | `1` | Ganancia mínima adicional en pips antes de que el trailing stop se mueva. |
| `UseTimeControl` | `true` | Habilita el filtro de sesión intradiaria. |
| `StartHour` | `7` | Hora de inicio de la sesión de trading (0-23). |
| `EndHour` | `17` | Hora de fin de la sesión de trading (1-24, exclusive). |
| `CloseOppositePositions` | `true` | Si está habilitado, revierte las posiciones existentes en una sola orden. |
| `WilliamsPeriod` | `360` | Período de retrospectiva para el indicador Williams %R. |
| `CandleType` | `1 minute` | Tipo de vela utilizado para evaluar Williams %R y las reglas de trading. |
| `PipSize` | `0.0001` | Valor de un solo pip en unidades de precio. |

## Notas adicionales
- La estrategia usa la API de suscripción de velas de alto nivel de StockSharp y la vinculación de indicadores para una lógica concisa sin gestión manual de buffers.
- Los cálculos de stop-loss, take-profit y seguimiento ocurren en velas completadas para evitar actuar sobre datos de precio no terminados.
- La implementación llama a `StartProtection()` una vez al arranque para alinearse con las directrices del proyecto, mientras que el manejo real del riesgo se gestiona dentro de la lógica de la estrategia.
- Ajuste el parámetro `PipSize` para que coincida con el instrumento negociado para que las distancias basadas en pips se mapeen correctamente a los movimientos de precio.
