# Diagrama de estrategia de dos activos con sus propias medias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama alinea velas finalizadas de quince minutos de BTCUSDT@BNBFT y TONUSDT@BNBFT, compara cada cierre con una media móvil simple de 20 períodos calculada para ese instrumento y usa las relaciones opuestas para gestionar exposición larga y corta en BTCUSDT. Un pestillo con signo gobernado por ejecuciones, reversiones a mercado en un solo paso, salidas ordinarias y un stop fijo local del 2% completan el flujo.

![schema](schema.svg)

## Resumen de la estrategia

- Variables separadas de instrumento configuran únicamente las suscripciones de velas de BTCUSDT y TONUSDT. Las acciones de órdenes y el flujo Strategy trades usan el Strategy Security seleccionado, que debe establecerse en BTCUSDT@BNBFT para coincidir con el parámetro Traded Security.
- Solo entran velas finalizadas de quince minutos en un bloque Sync. Cada par alineado proporciona BTC Close y BTC SMA(20) por una rama, y TON Close y TON SMA(20) por la otra; la decisión comienza únicamente cuando ambas medias están formadas.
- La relación larga exige estrictamente `BTC Close < BTC SMA(20)` junto con `TON Close > TON SMA(20)`. La relación corta exige estrictamente `BTC Close > BTC SMA(20)` junto con `TON Close < TON SMA(20)`. La igualdad no satisface ninguna relación completa.
- Un pestillo numérico con signo registra el estado BTC gestionado por el diagrama: `-1` significa corto, `0` plano y `1` largo. Desde plano, una relación completa envía una entrada a mercado de una unidad. Desde el estado opuesto, el volumen de la acción pasa a dos unidades, cierra la exposición existente de una unidad y establece una unidad en la nueva dirección mediante una sola acción de mercado.
- Una relación opuesta completa tiene prioridad sobre una salida ordinaria. En los demás casos, si BTC está estrictamente en el lado de salida de su propia media, se cierra la dirección actual mediante una acción ReduceOnly a mercado de una unidad. Cada decisión sincronizada termina antes de que el cierre BTC almacenado se entregue al stop fijo local del 2% ejecutado a mercado.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando un par sincronizado de velas finalizadas cumple `BTC Close < BTC SMA(20)` y `TON Close > TON SMA(20)`, la compuerta de compra acepta un pestillo plano o corto. Envía una compra NoCondition a mercado con Volume 1 desde plano, o con Volume 2 desde un corto de una unidad para revertir directamente a un largo BTC de una unidad.
- **Entrada en corto**: Cuando un par sincronizado de velas finalizadas cumple `BTC Close > BTC SMA(20)` y `TON Close < TON SMA(20)`, la compuerta de venta acepta un pestillo plano o largo. Envía una venta NoCondition a mercado con Volume 1 desde plano, o con Volume 2 desde un largo de una unidad para revertir directamente a un corto BTC de una unidad.
- **Salida**: Un largo se cierra con una venta ReduceOnly a mercado de una unidad cuando BTC Close está estrictamente por encima de BTC SMA(20) y no existe la relación corta completa. Un corto se cierra con una compra ReduceOnly a mercado de una unidad cuando BTC Close está estrictamente por debajo de BTC SMA(20) y no existe la relación larga completa. Esas comprobaciones de ausencia reservan una relación opuesta completa para la rama de reversión de dos unidades. La protección local también puede cerrar cualquier dirección con un stop fijo del 2% a mercado; Take Profit 0 desactiva el objetivo de beneficio.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Traded Security | BTCUSDT@BNBFT | Instrumento utilizado únicamente por la suscripción de velas del activo negociado. Establezca Strategy Security en el mismo valor BTCUSDT@BNBFT, porque todas las acciones de órdenes y el flujo Strategy trades usan Strategy Security. |
| Signal Security | TONUSDT@BNBFT | Instrumento utilizado únicamente por la segunda suscripción de velas. Su relación entre precio y media participa en las decisiones, pero ninguna acción de orden se dirige a esta variable. |
| BTC Candles Series | 00:15:00 | Serie de velas finalizadas de quince minutos de BTCUSDT usada para sincronización, BTC Close, BTC SMA(20), comprobaciones de protección y gráfico. |
| TON Candles Series | 00:15:00 | Serie de velas finalizadas de quince minutos de TONUSDT usada para sincronización, TON Close, TON SMA(20) y gráfico. |
| BTC SMA Length | 20 | Período de la SimpleMovingAverage calculada a partir de velas BTCUSDT sincronizadas y finalizadas. |
| TON SMA Length | 20 | Período de la SimpleMovingAverage calculada a partir de velas TONUSDT sincronizadas y finalizadas. |
| Base Volume | 1 | Cantidad predeterminada de entrada a mercado. El volumen de la acción es `Base Volume * (1 + abs(latch))`, por lo que una entrada desde plano usa Base Volume y una reversión usa el doble de Base Volume; con el valor predeterminado son Volume 1 y Volume 2. |
| Take Profit | 0 | Un valor absoluto de cero desactiva la protección de take-profit. |
| Stop Loss | 2% | Distancia porcentual adversa desde el precio de la ejecución protegida que activa el stop-loss. |
| Trailing Stop Loss | false | Desactivado, por lo que el stop del 2% permanece fijo en lugar de seguir un movimiento favorable del precio. |
| Use Market Orders | true | Activado, por lo que un stop disparado cierra la exposición protegida con una orden a mercado. |

## Detalles del diagrama

- Dos bloques [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) de tipo instrumento alimentan únicamente sus respectivos bloques [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html). Un bloque [Sincronización](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/sync.html) alinea los flujos finalizados en `00:15:00` antes de que cualquiera de las ramas llegue a la cadena de decisión.
- Cada vela sincronizada se divide en Close y un valor formado de [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html). Cuatro bloques de comparación estricta construyen las relaciones opuestas larga y corta con el Close de cada instrumento y su propia SMA(20).
- Una variable Unit numérica conserva el pestillo con signo. Las ejecuciones escriben `1` después de una compra, `-1` después de una venta y `0` después de una salida ordinaria o protectora. Las comparaciones de estado permiten entradas desde plano y reversiones desde el lado opuesto; la fórmula de volumen es `Base Volume * (1 + abs(latch))`.
- Las compuertas de las relaciones larga y corta accionan bloques [Modificar posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) configurados como NoCondition y MarketOrder. Acciones ReduceOnly a mercado separadas gestionan las salidas ordinarias. Cada compuerta de salida ordinaria también exige que la relación opuesta completa sea falsa, por lo que no se pueden solicitar una reversión y un cierre de una unidad para el mismo par sincronizado.
- La [Protección de posición](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) recibe ejecuciones de entradas, reversiones y salidas ordinarias. Take Profit es `0`, Stop Loss es `2%`, Trailing Stop Loss es `false`, Use Market Orders es `true` y la protección se ejecuta localmente. El cierre BTC sincronizado se almacena primero y solo se entrega a la protección después de que ambas ramas de señales hayan terminado su decisión para ese par.
- El gráfico recibe los flujos sincronizados de velas BTCUSDT y TONUSDT, BTC SMA(20), TON SMA(20), el flujo de órdenes stop-loss y todas las ejecuciones BTCUSDT de Strategy trades.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
