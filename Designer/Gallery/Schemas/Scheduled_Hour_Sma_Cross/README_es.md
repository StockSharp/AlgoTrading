# Cruce de SMA en una hora programada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama evalúa una media móvil rápida y otra lenta sobre velas finalizadas de treinta minutos de BTCUSDT, permite entradas programadas cuando la hora de apertura de la vela es 12, cierra posiciones contrarias fuera de esa hora y separa las acciones de mercado con una pausa de ocho velas.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de treinta minutos alimentan SMA(8) y SMA(21), que solo emiten valores formados. No se libera una decisión hasta que ambos valores existen para la misma vela.
- Una tendencia alcista significa `SMA(8) > SMA(21)` y una tendencia bajista significa `SMA(8) < SMA(21)`. Los valores iguales no producen ninguna acción.
- El bloque Time entrega la marca temporal de decisión y Converter extrae su Hour. En cada lote de vela finalizada, esa marca coincide con el `OpenTime` usado por la regla. Un Flag de un solo uso permite exactamente una decisión para esa vela, incluso durante eventos de órdenes síncronos.
- Un estado dirigido por ejecuciones registra la posición neta como `-1`, `0` o `1`. Cada una de las cuatro rutas prepara su propio estado siguiente antes de enviar una orden y solo lo confirma cuando esa ruta informa una ejecución.
- Cada acción inicia una pausa de ocho velas. Las velas posteriores 1 a 7 permanecen bloqueadas; la octava vela finalizada posterior reduce el contador a cero antes de decidir y vuelve a ser apta.

## Reglas de entrada y salida

- **Acción alcista programada**: En una vela cuyo `OpenTime.Hour` es 12, una tendencia alcista con posición plana o corta envía una compra a mercado de Volume 1. Una posición corta se reduce a cero y no se invierte.
- **Acción bajista programada**: En la misma hora, una tendencia bajista con posición plana o larga envía una venta a mercado de Volume 1. Una posición larga se reduce a cero y no se invierte.
- **Salida fuera de hora**: En cualquier otra hora de apertura, una tendencia bajista cierra una posición larga con una venta a mercado, mientras que una tendencia alcista cierra una posición corta con una compra a mercado. No se abre una posición nueva fuera de la hora 12.
- No existe una hora de cierre separada. Las cuatro ramas requieren que la pausa esté disponible y solo una rama verdadera puede activar su bloque Modify position independiente.

## Parámetros

| Parámetro | Valor predeterminado | Descripción |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrumento utilizado por Candles y Strategy trades. Configure Strategy Security con el mismo instrumento para las transacciones. |
| Candle Series | 00:30:00 | Intervalo de velas finalizadas y reloj de las actualizaciones de indicadores y pasos de pausa. |
| Fast SMA Length | 8 | Cantidad de velas finalizadas de la media móvil simple rápida. |
| Slow SMA Length | 21 | Cantidad de velas finalizadas de la media móvil simple lenta. |
| Trade Hour | 12 | Valor aceptado del campo `OpenTime.Hour` de la vela finalizada para las entradas programadas. |
| Cooldown N | 8 | Índice más temprano de una vela finalizada posterior en el que puede volver a considerarse una acción. |
| Volume | 1 | Cantidad fija de cada compra o venta a mercado. |

## Detalles del diagrama

- La [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) de BTC envía el instrumento a [Candles](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) construidas y finalizadas y a [Strategy trades](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html).
- Dos bloques [Indicator](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) de valores formados calculan SMA(8) y SMA(21). Los bloques Formula exponen sus valores numéricos a los bloques [Comparison](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) alcista y bajista.
- Time libera primero la posición guardada, la hora programada, la referencia cero, el estado de pausa, su marca temporal hacia el Converter de Hour y el volumen fijo. Activa al final el registro de decisión pendiente, de modo que cada puerta lógica de cinco entradas recibe una instantánea coherente de una vela.
- El Flag de un solo uso impide la reentrada durante el procesamiento síncrono de transacciones. Cuatro bloques [Modify position](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) separados evitan que una rama falsa consuma el volumen preparado para una rama verdadera.
- Los estados candidatos de compra y venta programadas son `posición + 1` y `posición - 1`; ambos candidatos de salida son cero. Un candidato llega al estado de posición únicamente mediante la salida `MyTrade` del Modify position correspondiente.
- El bloque [Delay](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) recibe cada vela antes de las decisiones de esa misma vela. Una puerta de acción verdadera marca primero la pausa como no disponible y arma N = 8; después envía la orden. El gráfico muestra velas, ambas medias, posición ejecutada, cuatro flujos de ejecuciones y todas las ejecuciones de la estrategia.

## Uso

Importe el archivo `.json` en Designer, configure Strategy Security como BTCUSDT@BNBFT, elija una cartera y ejecute el diagrama con historial de treinta minutos. Con los datos de marzo incluidos y los valores indicados, la validación produjo 59 órdenes de mercado finalizadas y 59 ejecuciones sin errores de transacción. Compruebe la zona horaria de las velas, el campo de hora, el volumen y la pausa antes de operar en vivo.
