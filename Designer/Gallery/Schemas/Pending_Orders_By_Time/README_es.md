# Diagrama de estrategia de ruptura virtual con órdenes pendientes por horario
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama utiliza velas finalizadas de cinco minutos para armar una vez al día dos niveles virtuales simétricos de ruptura. La vela con marca temporal 02:00 proporciona el cierre de referencia; un toque posterior de cualquiera de los niveles registra una única entrada a mercado, la protección porcentual gestiona la ejecución y la vela con marca temporal 22:00 desarma la configuración y cierra cualquier posición restante. Los niveles virtuales son valores almacenados, no órdenes en espera en el mercado.

![schema](schema.svg)

## Resumen de la estrategia

- Solo las velas finalizadas de cinco minutos controlan los horarios, los cálculos de niveles, las comprobaciones de ruptura y las actualizaciones del precio de protección. La ventana de apertura `02:00:00–02:04:59` selecciona exactamente la vela con marca temporal 02:00, que se procesa al completarse alrededor de las 02:05.
- Si no hay posición durante ese impulso de apertura, el diagrama almacena el cierre de la vela y calcula `Upper Level = Close × 1.0015` y `Lower Level = Close × 0.9985`. Ambos valores almacenados permanecen fijos y se sustituyen en el siguiente impulso de apertura válido.
- Una variable de estado armado y una cadena ordenada de disparadores de fin de vela garantizan que High, Low y los dos niveles almacenados se actualicen antes de tomar una decisión. Un bloque Flag compartido y de un solo uso admite únicamente la primera ruptura de la configuración diaria.
- La comparación superior se evalúa antes que la inferior. Si una vela abarca ambos niveles, solo se acepta la ruptura superior y el diagrama registra una única compra a mercado; en caso contrario, la ruptura inferior puede registrar una única venta a mercado. No existe ninguna orden antes de que se toque un nivel.
- Las ejecuciones de entrada inicializan la protección de posición. A continuación, los cierres de las velas finalizadas controlan sus comprobaciones de take-profit del 2% y stop-loss del 0.5%. La ventana de cierre `22:00:00–22:04:59` desarma cualquier configuración no utilizada y envía una acción ReduceOnly a mercado limitada a Order Volume 1; su ejecución vuelve al bloque de protección para restablecer su seguimiento de la exposición.

## Reglas de entrada y salida

- **Entrada en largo**: Mientras el par virtual está armado, `High ≥ Upper Level` supera la compuerta diaria de un solo uso y registra una compra a mercado por Order Volume 1. El mismo evento desactiva ambas rutas de ruptura hasta el siguiente impulso de apertura válido.
- **Entrada en corto**: Si no se aceptó la ruptura superior, una condición armada `Low ≤ Lower Level` supera la compuerta de un solo uso y registra una venta a mercado por Order Volume 1. También desactiva ambas rutas de ruptura durante el resto del ciclo.
- **Salida**: La protección de posición envía una salida a mercado cuando el cierre de una vela finalizada alcanza un movimiento del 2% a favor del precio real de ejecución de la entrada o del 0.5% en su contra. De forma independiente, la vela con marca temporal 22:00 desarma el par virtual y solicita un cierre ReduceOnly a mercado de, como máximo, Order Volume 1. No se actúa sobre un umbral de protección intravela que no esté presente al cierre de la vela finalizada, y la configuración no vuelve a armarse después de una salida hasta la siguiente ventana de apertura.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles Series | 00:05:00 | Velas finalizadas de cinco minutos utilizadas para los horarios, los niveles virtuales, las pruebas de ruptura y las comprobaciones de protección basadas en el precio de cierre. |
| Opening Window | 02:00:00–02:04:59 | Intervalo inclusivo de una sola vela que captura el cierre de referencia cuando no hay posición. |
| Closing Window | 22:00:00–22:04:59 | Intervalo inclusivo de una sola vela que desarma una configuración no utilizada y cierra una posición abierta. |
| Entry Distance | 0.15% | Desplazamiento porcentual simétrico por encima y por debajo del cierre capturado. |
| Take Profit | 2% | Movimiento favorable del precio de cierre desde el precio real de ejecución de la entrada que activa la protección. |
| Stop Loss | 0.5% | Movimiento adverso del precio de cierre desde el precio real de ejecución de la entrada que activa la protección. |
| Order Volume | 1 | Cantidad enviada por cualquiera de las entradas a mercado y reducción máxima programada de la posición. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas finalizadas de cinco minutos. Los bloques [Conversor](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/converter.html) de Close, High y Low proporcionan flujos numéricos explícitos.
- Dos bloques [Horario de trabajo](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/time/working_time.html) inspeccionan OpenTime de la vela. Sus límites superiores terminan un segundo antes de la siguiente marca de cinco minutos porque ambos límites configurados son inclusivos.
- Los bloques [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) almacenan el cierre de referencia, los niveles calculados, el estado armado, el lado elegido y las constantes. Dos bloques [Fórmula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) calculan los desplazamientos porcentuales simétricos únicamente durante un impulso de apertura válido.
- Los bloques [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html), [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) y [Flag](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/flag.html) imponen el armado únicamente sin posición, la evaluación con valores actualizados, la precedencia de compra y una sola ruptura aceptada por configuración.
- Los bloques de compra y venta [Registro de órdenes](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/orders/register.html) son acciones de órdenes a mercado. Sus salidas MyTrade inicializan el bloque compartido [Protección de posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), cuya entrada Price recibe los cierres de las velas finalizadas.
- El bloque programado [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) utiliza ReduceOnly con Order Volume 1. Obtiene la dirección de cierre de la exposición actual, nunca aumenta la posición y devuelve su salida MyTrade a la protección de posición después de una salida programada.
- El [Panel de gráfico](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) muestra las velas finalizadas, los dos niveles virtuales almacenados, las órdenes de entrada y protección, y las ejecuciones de entrada, protección y cierre programado.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
