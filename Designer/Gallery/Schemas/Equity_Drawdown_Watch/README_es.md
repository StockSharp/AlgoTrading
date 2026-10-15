# Monitor de drawdown del capital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama construye una curva de capital a partir del P&L de la estrategia, conserva un máximo persistente, registra una sola vez cada nuevo cruce del umbral de drawdown y ejecuta un ciclo largo protegido deliberadamente espaciado para que los valores supervisados de la cuenta cambien durante el backtest.

![schema](schema.svg)

## Descripción de la estrategia

- Las velas BTCUSDT de cinco minutos finalizadas proporcionan el único reloj de muestreo. Una suscripción Level 1 paralela al mejor precio de compra mantiene activa la valoración del P&L no realizado entre muestras de velas.
- P&L change actualiza almacenes silenciosos de P&L realizado y no realizado a su propio ritmo de eventos. Cada vela finalizada libera una vez ambos valores recientes junto con Start Balance hacia `Equity = Start Balance + Realized P&L + Unrealized P&L`.
- `max(máximo guardado, capital)` mantiene el máximo de capital de toda la ejecución. Highest(2), configurado para emitir solo valores formados, confirma este flujo máximo monótono e introduce una muestra de calentamiento sin acortar su historial.
- El drawdown se calcula como `(Peak - Equity) / Peak * 100`. Comparison comprueba `Drawdown >= Drawdown Alert`, mientras Crossing y un almacén booleano envían al registro únicamente un nuevo cruce ascendente del umbral.
- Modify position abre una posición larga a mercado cuando no hay posición. La protección de distancia absoluta la cierra y un temporizador de 1.440 velas permite la siguiente entrada solo después de cinco días de velas posteriores de cinco minutos.

## Reglas de entrada y salida

- **Entrada larga**: una vez formado Highest(2), un Flag de entrada disponible envía Volume 1 a un bloque Modify position con Buy, OpenPosition y MarketOrder. Por ello, el primer intento de entrada ocurre en la segunda vela finalizada.
- **Entrada corta**: el diagrama no abre posiciones cortas. Las operaciones Sell son salidas protectoras de la posición larga.
- **Salida**: Position protection envía una salida a mercado tras un movimiento favorable de 0.04 o desfavorable de 0.03 en unidades absolutas de precio. La ejecución de entrada inicia el enfriamiento de las 1.440 velas posteriores; el temporizador, no la ejecución de salida, restablece la disponibilidad de entrada.

## Parámetros

| Parámetro | Valor predeterminado | Descripción |
|---|---|---|
| BTC Data Security | BTCUSDT@BNBFT | Instrumento usado por las suscripciones Candles, Level 1 y Strategy trades. Establezca Strategy Security en el mismo instrumento para las transacciones y el P&L. |
| Candle Series | 00:05:00 | Intervalo de velas finalizadas y reloj para las muestras de capital y los pasos del enfriamiento. |
| Start Balance | 1000 | Importe base añadido al P&L realizado y no realizado al calcular el capital. |
| Peak Confirmation Length | 2 | Longitud de Highest sobre el flujo de máximo persistente monótono; retrasa la negociación hasta la segunda muestra. |
| Drawdown Alert, % | 1 | Se escribe un registro cuando el drawdown alcanza o supera este nivel desde abajo. |
| Volume | 1 | Cantidad de cada entrada larga. |
| Entry Cooldown N | 1440 | Número de velas posteriores finalizadas de cinco minutos entre entradas permitidas, equivalente a cinco días. |
| Take Distance | 0.04 | Distancia absoluta favorable del precio que cierra la posición larga a mercado. |
| Stop Distance | 0.03 | Distancia absoluta desfavorable del precio que cierra la posición larga a mercado. |

## Detalles del diagrama

- La [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) BTC alimenta [Candles](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) finalizadas, [Level 1](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/level_1.html) de mejor precio de compra y [Strategy trades](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/trades_by_strategy.html). Los bloques de negociación usan Strategy Security y Strategy Portfolio.
- Los almacenes Variable silenciosos separan el flujo dirigido por eventos de [P&L change](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/pnl_strategy.html) del reloj de velas. Su orden fijo de liberación proporciona a cada [Formula](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/formula.html) un conjunto completo de entradas de la misma vela.
- El máximo persistente comienza en cero, por lo que los cambios de Start Balance siguen siendo válidos. La Formula max actualiza este estado antes de que su salida monótona llegue al [Indicator](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) Highest(2) formado.
- [Comparison](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) y [Crossing](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) reciben el umbral y el drawdown en un orden fijo. Se ignora el cruce false de recuperación; un cruce ascendente true libera el porcentaje guardado mediante [String Formatter](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/string_format.html) hacia una [Notification](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/notifying/notification.html) Log.
- El bloque de enfriamiento [Delay](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html) consume cada vela antes de cualquier decisión de esa misma vela. Una ejecución Buy activa N = 1440 y su salida restablece el Flag de entrada antes de que la vela habilitada llegue a Highest.
- La ejecución Buy de [Modify position](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/modify.html) inicializa la [Position protection](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/positions/protect.html) local. El gráfico muestra velas, capital muestreado, máximo, drawdown, ambos componentes de P&L, ejecuciones de entrada, ejecuciones protectoras y todas las ejecuciones de la estrategia.

## Uso

Importe el archivo `.json` en Designer, establezca Strategy Security en BTCUSDT@BNBFT y ejecútelo sobre el historial de marzo incluido. Antes de usar el diagrama en negociación real, compruebe para su instrumento la escala del capital, las distancias absolutas de protección, el porcentaje de alerta y el enfriamiento de cinco días.
