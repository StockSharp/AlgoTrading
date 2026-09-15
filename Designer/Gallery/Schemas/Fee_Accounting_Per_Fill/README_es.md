# Diagrama de contabilización de comisión por ejecución
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera retornos confirmados del CCI(30) horario a sus umbrales mediante una orden de mercado de volumen fijo y muestra la contabilización de comisión para cada ejecución observada. Un enfriamiento de cuatro velas controla las señales nuevas, una capa educativa de protección porcentual puede cerrar la posición y un solo registro recibe tanto el cargo calculado por ejecución como la comisión acumulada del motor de la estrategia.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de una hora alimentan CommodityChannelIndex 30. El indicador emite todos los valores, incluso los producidos antes de completar su longitud.
- Una compra exige CCI anterior < -100 y CCI actual >= -100. Una venta exige CCI anterior > 100 y CCI actual <= 100.
- La compra también exige Position <= 0, la venta exige Position >= 0 y ambas requieren que el enfriamiento de cuatro velas esté listo.
- Cada señal aceptada envía exactamente una orden de mercado de Volume fijo. Desde una posición plana abre el lado indicado; frente a una posición unitaria contraria la cierra hasta cero y no abre el otro lado con la misma señal.
- La protección de posición es una capa educativa explícita con take-profit del 1% y stop-loss fijo del 0,7%. Solo comprueba cierres de velas finalizadas y omite el cierre de cualquier vela que ya haya producido una orden de señal.
- Cada ejecución observada produce un cargo simulado mediante Trade.Price × Trade.Volume × Commission Rate % / 100. El gráfico muestra CCI, órdenes, ejecuciones y ambas series de comisión, mientras los mensajes formateados se escriben en un único flujo de registro.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando el CCI anterior está por debajo de -100, el CCI actual vuelve a -100 o más, Position <= 0 y el enfriamiento está listo, se envía una compra de mercado de Volume. Desde posición plana abre un largo; frente a un corto unitario solo lo cierra hasta cero.
- **Entrada en corto**: Cuando el CCI anterior está por encima de 100, el CCI actual vuelve a 100 o menos, Position >= 0 y el enfriamiento está listo, se envía una venta de mercado de Volume. Desde posición plana abre un corto; frente a un largo unitario solo lo cierra hasta cero.
- **Salida**: Una señal CCI contraria y válida puede llevar una posición unitaria a cero con una orden de mercado de volumen fijo. De forma independiente, la capa educativa de protección puede cerrar la exposición seguida con un take-profit del 1% o un stop-loss fijo del 0,7%; su entrada de precio solo recibe cierres finalizados de velas sin orden de señal.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 01:00:00 | Marco temporal de una hora; solo las velas finalizadas impulsan decisiones CCI, incrementos del enfriamiento y comprobaciones de precio de la protección. |
| CCI Length | 30 | Longitud de CommodityChannelIndex; el bloque emite valores sin esperar a que el indicador esté completamente formado. |
| Lower Level | -100 | Umbral inferior del CCI. El retorno ascendente a través de -100 crea la condición de compra. |
| Upper Level | 100 | Umbral superior del CCI. El retorno descendente a través de 100 crea la condición de venta. |
| Cooldown | 4 | Número de velas finalizadas necesarias después de una ejecución antes de permitir otra operación por señal. |
| Commission Rate % | 0.04 | Tasa porcentual usada solo por la fórmula mostrada por ejecución `Trade.Price × Trade.Volume × rate / 100`. |
| Take Profit % | 1 | Movimiento porcentual favorable empleado por la capa educativa de protección de posición. |
| Stop Loss % | 0.7 | Movimiento porcentual adverso empleado por el stop fijo, no móvil, de la capa educativa de protección. |
| Volume | 1 | Cantidad fija de cada orden de señal de compra o venta. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite velas horarias finalizadas. Su cierre se guarda para la protección y un bloque [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calcula CCI 30 con el filtro de valores formados desactivado. Banderas por vela conservan las cuatro comparaciones del valor anterior y actual con los umbrales hasta el pulso de decisión final.
- [Posición actual](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) aporta las compuertas Position <= 0 y Position >= 0. El enfriamiento comienza en 4, se incrementa y limita antes de cada decisión de vela, y se reinicia a 0 con cada ejecución directa de una orden de señal o de protección. Por tanto, las velas finalizadas 1, 2 y 3 posteriores quedan bloqueadas y la vela 4 ya es válida.
- Dos bloques de [Registro de órdenes](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/orders/register.html) envían la compra y la venta de mercado con Volume fijo. La salida directa de ejecución de cada bloque actualiza la protección y reinicia el enfriamiento; un bloque Trades for order dedicado observa la Order registrada y aporta el flujo de ejecuciones de señal al cálculo simulado de comisión y al gráfico.
- Ambos flujos directos de ejecuciones de señal entran en [Protección de posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/protect.html), de modo que su posición interna vuelve a cero tras un cierre por señal. Su propia salida de ejecución no retorna a esa entrada. La compuerta sin señal libera el cierre finalizado guardado para comprobar la protección solo si ninguna orden de señal se activó en esa vela.
- Para cada ejecución observada de compra, venta o protección, los convertidores guardan Trade.Price y Trade.Volume en pestillos silenciosos. Después, un pulso libera tasa, precio y volumen en ese orden; la [Fórmula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) actualiza `a × b × r / 100` y el estado silencioso de la comisión se libera al final exactamente una vez para esa ejecución observada.
- La salida Commission de [Ganancias y pérdidas de la estrategia](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) es la comisión acumulada del motor y permanece en cero cuando el entorno de prueba o ejecución no tiene configurada una regla de comisión. La fórmula simulada por ejecución solo se muestra y no escribe en ese valor del motor.
- Las salidas de dos [Formateadores de cadenas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) se combinan mediante Combination<IComparable> y se envían a una [Notificación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) Log. Trades for order se suscribe después de recibir la Order registrada, por lo que un entorno que complete una orden dentro de la llamada de registro puede producir una ejecución antes de conectar el observador; la salida directa de ejecución seguirá controlando protección y enfriamiento.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
