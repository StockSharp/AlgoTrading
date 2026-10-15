# Diagrama de estrategia con tendencia del instrumento de referencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama primero sincroniza las velas finalizadas de cinco minutos de BTCUSDT@BNBFT y TONUSDT@BNBFT, y después negocia cruces exactos de las EMA de BTC cuando la tendencia EMA de TON confirma la misma dirección. Los filtros de posición y pausa controlan cada decisión y dos rutas de órdenes de mercado con volumen fijo gestionan la exposición.

![schema](schema.svg)

## Resumen de la estrategia

- El bloque Sync recibe los dos flujos de velas finalizadas de cinco minutos con Interval `00:05:00` y ClearSockets activado. Solo emite una pareja BTC–TON alineada cuando están presentes ambas velas; si falta una, el intervalo incompleto se descarta.
- Cada pareja alineada alimenta después la EMA rápida 7 y la EMA lenta 18 de BTC, y la EMA rápida 47 y la EMA lenta 50 de TON. El filtrado exclusivo de valores formados está desactivado en los cuatro indicadores.
- Un cruce alcista de BTC exige `PrevFast <= PrevSlow` y `Fast > Slow`; un cruce bajista exige `PrevFast >= PrevSlow` y `Fast < Slow`. La relación actual de TON confirma compras con `Fast > Slow` y ventas con `Fast < Slow`.
- La ruta de compra también exige `Position <= 0`, mientras que la ruta de venta exige `Position >= 0`. Ambas rutas envían órdenes de mercado `NoCondition` con Volume fijo de 1.
- Las primeras cinco parejas sincronizadas BTC–TON quedan bloqueadas y cada señal de orden bloquea las cinco parejas sincronizadas siguientes; la sexta pareja alineada vuelve a ser apta. No hay stop-loss, take-profit ni bloque de salida separado, y el gráfico muestra velas de BTC, ambas EMA de BTC y ambos flujos de ejecuciones.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando BTC cumple `PrevFast <= PrevSlow` y `Fast > Slow`, TON tiene actualmente `Fast > Slow`, la comprobación sincronizada es `Position <= 0` y la pausa ha terminado, el diagrama envía una compra a mercado con Volume 1.
- **Entrada en corto**: Cuando BTC cumple `PrevFast >= PrevSlow` y `Fast < Slow`, TON tiene actualmente `Fast < Slow`, la comprobación sincronizada es `Position >= 0` y la pausa ha terminado, el diagrama envía una venta a mercado con Volume 1.
- **Salida**: No hay bloque dedicado de salida ni de protección. Una orden posterior apta en la dirección contraria reduce la exposición; desde una posición `+1` o `-1`, el Volume fijo de 1 lleva la posición a cero en vez de abrir el lado contrario.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Main Fast EMA | 7 | Período de la ExponentialMovingAverage rápida calculada con velas finalizadas de cinco minutos de BTCUSDT@BNBFT; el filtro exclusivo de valores formados está desactivado. |
| Main Slow EMA | 18 | Período de la ExponentialMovingAverage lenta calculada con velas finalizadas de cinco minutos de BTCUSDT@BNBFT; el filtro exclusivo de valores formados está desactivado. |
| Reference Fast EMA | 47 | Período de la ExponentialMovingAverage rápida calculada con la vela finalizada y alineada de cinco minutos de TONUSDT@BNBFT; el filtro exclusivo de valores formados está desactivado. |
| Reference Slow EMA | 50 | Período de la ExponentialMovingAverage lenta calculada con la vela finalizada y alineada de cinco minutos de TONUSDT@BNBFT; el filtro exclusivo de valores formados está desactivado. |
| Cooldown Bars | 5 | Número de parejas de velas sincronizadas iniciales y posteriores a una señal que se bloquean antes de habilitar la siguiente pareja alineada. |
| Volume | 1 | Cantidad fija suministrada a los dos bloques de órdenes de mercado NoCondition. |

## Detalles del diagrama

- Dos bloques [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) envían las velas finalizadas de cinco minutos de BTCUSDT@BNBFT y TONUSDT@BNBFT directamente a la sincronización.
- El bloque [Sync](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/sync.html) alinea las dos entradas de velas con Interval `00:05:00` y ClearSockets `true`. Libera ambas velas como una pareja; si falta cualquiera de ellas, el intervalo incompleto se limpia sin entrar en la cadena de indicadores.
- Solo la pareja sincronizada alimenta cuatro bloques [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html): ExponentialMovingAverage 7 y 18 para BTC y 47 y 50 para TON. Su opción exclusiva de valores formados es `false`, y solo las dos salidas EMA de BTC también se envían al gráfico.
- Los bloques [Valor anterior](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html) conservan los valores previos sincronizados de las EMA rápida y lenta de BTC. Los bloques de [Comparación](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) expresan ambos lados de cada cruce exacto y las dos relaciones actuales de tendencia de TON; rutas separadas de [Condición lógica](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) los combinan para comprar y vender.
- La [Posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/current.html) actual aporta las comprobaciones `Position <= 0` y `Position >= 0`. La puerta [N values](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) recibe la salida sincronizada de velas BTC y usa N=5 para suprimir las primeras cinco parejas alineadas y las cinco posteriores a cada señal de orden, y vuelve a habilitar decisiones en la sexta.
- Los bloques de compra y venta [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) colocan órdenes de mercado con `NoCondition` y Volume 1 compartido. No hay stop-loss, take-profit, protección de posición ni elemento de salida independiente.
- El [Panel de gráfico](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/chart.html) recibe velas finalizadas de BTC, BTC EMA 7, BTC EMA 18 y las salidas MyTrade de los bloques de compra y venta.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
