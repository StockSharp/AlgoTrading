# Estrategia de Stop Loss Global y Ventana de Negociación
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia replica el comportamiento del experto de MetaTrader **Exp_GStopLoss_Tm**, proporcionando una capa de riesgo superpuesta que monitorea el resultado combinado de todas las operaciones abiertas por la instancia de estrategia. El módulo no genera señales de entrada por sí mismo; en cambio, rastrea las ganancias y pérdidas de las posiciones existentes y aplica tanto un umbral de stop loss global como una ventana de sesión de negociación opcional. Cuando las pérdidas superan el límite configurado o el mercado se mueve fuera del rango de tiempo permitido, la estrategia liquida la exposición actual y bloquea cualquier operación posterior hasta que el libro quede plano nuevamente.

## Lógica de trading
1. Al inicio, la estrategia registra el PnL realizado actual como referencia base. Esto le permite medir el beneficio flotante relativo al estado plano más reciente.
2. Cada vela terminada producida por el tipo de vela configurado activa una verificación de riesgo. El marco temporal predeterminado es un minuto para emular la vigilancia a nivel de tick sin sobrecargar el sistema.
3. El módulo calcula el beneficio no realizado como la diferencia entre el PnL actual de la estrategia y el valor base. El PnL positivo se ignora mientras la estrategia permanece dentro de la ventana de negociación, en concordancia con el asesor experto original.
4. Si el modo de pérdida está configurado como **Percent**, la estrategia compara el porcentaje de pérdida absoluta contra la equidad de la cuenta obtenida de `Portfolio.CurrentValue`. Para el modo **Currency**, la comparación se realiza en unidades de moneda absolutas.
5. Una vez que se supera el umbral de pérdida, el indicador de stop se activa y la estrategia comienza a cerrar la posición abierta en la siguiente iteración. El indicador solo se libera después de que el tamaño de la posición vuelva a cero y el PnL base se actualice.
6. Cuando la ventana de negociación opcional está habilitada, la verificación de riesgo también evalúa si el tiempo de cierre de la vela se encuentra dentro del intervalo permitido. La ventana admite sesiones intradía que se envuelven alrededor de la medianoche, espejando la lógica de MetaTrader.
7. Siempre que el indicador de stop esté activo o el filtro de sesión detecte que el mercado está fuera del horario permitido, el módulo envía una orden de mercado en la dirección opuesta para aplanar la posición. Las entradas de registro informativas describen la razón de cada salida.

## Parámetros
| Nombre | Descripción |
| ---- | ----------- |
| `LossMode` | Selecciona cómo se interpreta el umbral de pérdida: porcentaje de la equidad de la cuenta actual o moneda de la cuenta absoluta. |
| `StopLoss` | Valor del umbral de pérdida. Para el modo porcentaje el número representa el porcentaje, mientras que el modo moneda usa la moneda de la cuenta. |
| `UseTimeFilter` | Habilita la ventana de negociación intradía. Cuando está deshabilitado, la estrategia ignora el filtro de tiempo completamente. |
| `StartTime` | Inicio inclusivo de la ventana de negociación en UTC. Funciona junto con `EndTime` para definir la sesión válida. |
| `EndTime` | Fin exclusivo de la ventana de negociación en UTC. Admite sesiones envolventes cuando el tiempo de fin es anterior al de inicio. |
| `CandleType` | Suscripción de velas usada para impulsar la evaluación de riesgo periódica. El valor predeterminado es un marco temporal de 1 minuto. |

## Notas de implementación
- El PnL base se recalcula cada vez que el tamaño de la posición vuelve a cero, de modo que las operaciones posteriores comiencen con la pizarra limpia.
- Los valores de equidad se obtienen del portafolio en vivo, por lo tanto el modo porcentaje se adapta tanto a los cambios realizados como no realizados en el valor de la cuenta.
- Todos los comentarios en el código fuente están escritos en inglés según lo requerido por las convenciones del proyecto.
- La estrategia dibuja velas y operaciones propias en el área del gráfico predeterminada cuando hay una disponible, ayudando a visualizar el comportamiento durante las pruebas.

## Pautas de uso
1. Adjunte la estrategia al instrumento que desea supervisar. La generación de órdenes de otras estrategias aún puede ocurrir; este módulo solo monitorea y cierra posiciones.
2. Configure el modo de pérdida y el umbral que coincida con su apetito de riesgo. Por ejemplo, `LossMode = Percent` y `StopLoss = 5` cerrarán la posición después de un 5% de caída no realizada relativa a la equidad actual.
3. Establezca los parámetros `StartTime` y `EndTime` para limitar el trading a una sesión intradía particular. Para cubrir una ventana nocturna, especifique una hora de inicio posterior a la hora de fin (por ejemplo 20:00 a 06:00).
4. Ejecute el backtest o la sesión en vivo. La estrategia reiniciará automáticamente el indicador de stop una vez que todas las posiciones estén aplanadas y continuará supervisando operaciones posteriores.
