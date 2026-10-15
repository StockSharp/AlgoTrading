# Diagrama de estrategia con alertas de cruces de EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama opera los cruces alcistas y bajistas de una EMA rápida de 120 períodos y una EMA lenta de 450 períodos sobre velas finalizadas de un minuto. Una captura de la posición filtra cada señal, las órdenes de mercado de volumen fijo gestionan la exposición, cada ejecución propia se escribe en el registro y el gráfico muestra las velas, ambas EMA y las ejecuciones de compra y venta.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de un minuto alimentan la EMA rápida 120 y la EMA lenta 450. El filtro de valores formados está desactivado en ambos indicadores, por lo que sus valores están disponibles desde el inicio del cálculo.
- El bloque Crossing emite `true` cuando la EMA rápida cruza hacia arriba la EMA lenta. Un bloque NOT convierte el evento `false` del cruce bajista en el disparador positivo del lado vendedor.
- Al evaluar cada vela, una captura accionada por la vela emite la posición actual antes de procesar las señales de EMA. Las comparaciones solo permiten comprar con `Position <= 0` y vender con `Position >= 0`.
- Los dos bloques de órdenes de mercado usan `NoCondition` y un Volume fijo de 1. Una señal opuesta puede reducir o dejar a cero una posición y puede atravesar el cero cuando su magnitud es menor que Volume, pero no garantiza una reversión completa.
- El bloque Strategy trades envía cada ejecución propia a una notificación Log mediante la plantilla exacta del mensaje de ejecución. El gráfico recibe las velas finalizadas, ambas EMA y los flujos de ejecuciones de compra y venta.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando la EMA rápida cruza por encima de la EMA lenta y la captura de posición de la vela es menor o igual que cero, el diagrama envía una compra a mercado con Volume 1.
- **Entrada en corto**: Cuando la EMA rápida cruza por debajo de la EMA lenta y la captura de posición de la vela es mayor o igual que cero, el diagrama envía una venta a mercado con Volume 1.
- **Salida**: No existe un bloque dedicado de salida ni de protección. Una orden posterior de volumen fijo en la dirección opuesta puede reducir la posición actual, cerrar una posición del mismo tamaño o atravesar el cero cuando la posición es menor que Volume; no se garantiza una reversión completa.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles | 00:01:00 | Marco temporal de un minuto; solo las velas finalizadas impulsan las EMA y la cadena de decisión. |
| Fast EMA Period | 120 | Período de la ExponentialMovingAverage rápida; el filtro de valores formados está desactivado. |
| Slow EMA Period | 450 | Período de la ExponentialMovingAverage lenta; el filtro de valores formados está desactivado. |
| Volume | 1 | Cantidad fija suministrada a los dos bloques de órdenes de mercado con NoCondition. |

## Detalles del diagrama

- El bloque [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) emite únicamente velas finalizadas de un minuto y activa la captura de posición antes de alimentar los cálculos de EMA.
- Dos bloques [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) calculan ExponentialMovingAverage con períodos 120 y 450. Su opción de valores formados es `false` y ambas salidas también se envían al gráfico.
- La salida de [Cruce](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) es `true` en un cruce alcista y `false` en uno bajista. Una [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) NOT convierte el evento bajista en un disparador positivo de venta; bloques AND separados combinan dirección y posición.
- La [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual se guarda continuamente y se emite una vez por vela antes de la ruta de las EMA. Los bloques de [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) evalúan `Position <= 0` y `Position >= 0` en la misma cadena causal de la vela que el cruce.
- Los bloques de compra y venta [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) colocan órdenes de mercado con `NoCondition` y el valor fijo Volume compartido. No hay stop, objetivo de beneficios ni bloque de salida independiente.
- [Operaciones de la estrategia](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html) emite cada `MyTrade` propia. El [Formateador de cadenas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) usa exactamente `EMA cross fill: {Order.Side} {Trade.TradeVolume:0.########} {Order.Security.Id} @ {Trade.TradePrice:0.########}`.
- El bloque [Notificación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) escribe cada ejecución formateada con Type `Log` y Caption `EMA cross trade`. El gráfico representa las velas, la EMA rápida, la EMA lenta y los flujos separados de ejecuciones de compra y venta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
