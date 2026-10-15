# Diagrama de estrategia de cruce de canal con protección de P&L
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama calcula el punto medio de un canal de 24 velas a partir de velas de cinco minutos finalizadas de BTCUSDT@BNBFT y opera los cruces exactos entre el cierre y el punto medio después de una pausa de 200 velas. La profundidad de mercado de TONUSDT@BNBFT confirma que el flujo está listo y marca los momentos de comprobación del P&L no realizado; una protección monetaria de un solo disparo solicita cancelar cualquier orden de entrada que siga activa y cierra la posición de BTC al alcanzar cualquiera de los umbrales configurados.

![schema](schema.svg)

## Descripción de la estrategia

- La variable BTC Security configura la suscripción a velas finalizadas de cinco minutos. Las órdenes de mercado, ejecuciones, cierre de posición y P&L pertenecen al Strategy Security seleccionado, que debe establecerse en BTCUSDT@BNBFT para coincidir con BTC Security.
- Highest(24) recibe las velas de BTC y sigue sus máximos, mientras que Lowest(24) sigue sus mínimos. El punto medio aritmético es `(Highest + Lowest) / 2`; la primera decisión disponible guarda el cierre y el punto medio, inicia la primera pausa y no envía una orden.
- Un cruce alcista requiere `Previous Close <= Previous Midpoint` y `Current Close > Current Midpoint`. Un cruce bajista requiere `Previous Close >= Previous Midpoint` y `Current Close < Current Midpoint`. Los valores guardados avanzan con cada vela finalizada de BTC, incluidas las velas rechazadas por la pausa.
- Una entrada o inversión solo puede ejecutarse después de 200 velas finalizadas de BTC estrictamente posteriores y tras recibir al menos un evento de profundidad de mercado de TON. Cada cruce aceptado reinicia el retardo de 200 velas antes de enviar su orden de mercado.
- Un pestillo con signo actualizado por ejecuciones registra el estado gestionado de BTC: `-1` es corto, `0` es sin posición y `1` es largo. Un P&L no realizado igual o superior a `500`, o igual o inferior a `-300`, dispara una protección de una sola vez; una ejecución posterior de compra o venta vuelve a armarla.

## Reglas de entrada y salida

- **Entrada larga**: Cuando aparece el cruce alcista exacto, el pestillo con signo está sin posición o corto, ambas puertas de disponibilidad están abiertas y la pausa ha terminado, se envía una compra a mercado. La entrada sin posición usa Base BTC Volume; la inversión de corto a largo usa el doble de esa cantidad.
- **Entrada corta**: Cuando aparece el cruce bajista exacto, el pestillo con signo está sin posición o largo, ambas puertas de disponibilidad están abiertas y la pausa ha terminado, se envía una venta a mercado. La entrada sin posición usa Base BTC Volume; la inversión de largo a corto usa el doble de esa cantidad.
- **Salida**: Un cruce contrario elegible realiza una inversión normal en un solo paso en lugar de un cierre separado. De forma independiente, la protección de P&L se activa con `P&L >= Profit Target` o `P&L <= -abs(Maximum Loss)`, envía una intención de cancelación masiva más solicitudes de cancelación dirigidas a las órdenes de entrada guardadas y solicita un cierre a mercado. Este cierre monetario no reinicia la pausa de cruces.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|---|---|---|
| BTC Security | BTCUSDT@BNBFT | Instrumento usado por la suscripción a velas de cinco minutos. Establezca Strategy Security en el mismo valor porque las acciones de órdenes, ejecuciones, cierre de posición y P&L usan Strategy Security. |
| TON Readiness Security | TONUSDT@BNBFT | Instrumento usado solo por la suscripción de profundidad de mercado que abre la puerta de disponibilidad del flujo y marca el muestreo del P&L; sus precios de cotización no se usan en órdenes de BTC ni en la valoración del P&L. |
| Candle Series | 00:05:00 | Serie de velas finalizadas de cinco minutos de BTCUSDT utilizada para el canal, las decisiones de cruce exacto, el conteo de la pausa y el gráfico. |
| Highest Length | 24 | Número de velas de BTC que Highest utiliza para calcular el límite superior del canal. |
| Lowest Length | 24 | Número de velas de BTC que Lowest utiliza para calcular el límite inferior del canal. |
| Base BTC Volume | 1 | Cantidad predeterminada de entrada a mercado. La fórmula es `Base BTC Volume * (1 + abs(latch))`, por lo que una entrada sin posición usa la cantidad base y una inversión usa el doble. |
| Cooldown N | 200 | Número de velas finalizadas de BTC estrictamente posteriores requeridas después de la inicialización o de un cruce aceptado para que otro cruce pueda enviar una orden. |
| Profit Target | 500 | Nivel de P&L no realizado en el que, o por encima del cual, la protección de un solo disparo solicita cancelación y cierre a mercado. |
| Maximum Loss | 300 | Magnitud positiva de pérdida; el umbral de protección se calcula como `-abs(Maximum Loss)`, que es `-300` de forma predeterminada. |

## Detalles del diagrama

- La [Variable](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html) BTC alimenta únicamente la suscripción de [Velas](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html) finalizadas. La variable TON alimenta solo la [Profundidad de mercado](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html); su primer evento activa el pestillo de disponibilidad y los siguientes también marcan el muestreo del último P&L no realizado.
- Dos bloques de [Indicador](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) reciben cada vela de BTC. Highest(24), Lowest(24), una fórmula de punto medio y los pestillos de valores actuales y anteriores conservan una decisión completa de cierre y canal por cada vela finalizada.
- Un bloque Delay comienza durante la primera decisión y se reinicia con cada cruce aceptado. Como la vela actual llega a su entrada antes de ejecutarse la rama de decisión, la elegibilidad solo vuelve tras 200 velas finalizadas de BTC posteriores; los cruces rechazados aun así reemplazan el cierre y el punto medio guardados.
- Los bloques de [Registro de órdenes](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/orders/register.html) de compra y venta envían órdenes de mercado con `Base BTC Volume * (1 + abs(latch))`. Sus salidas MyTrade escriben el estado con signo y vuelven a armar la protección de P&L a partir de ejecuciones reales.
- Los eventos de cambio de P&L y de profundidad de TON muestrean el último P&L no realizado. Las comparaciones estrictas de umbral alimentan una puerta armada compartida, de modo que alcanzar `500` o `-300` solo puede producir una acción protectora hasta que una ejecución de entrada posterior la vuelva a armar.
- La protección invoca la [Cancelación masiva de órdenes](https://doc.stocksharp.com/es/topics/designer/strategies/using_visual_designer/elements/orders/mass_cancel.html), entrega las referencias Order de compra y venta guardadas a bloques de cancelación dirigida y usa Base BTC Volume para cerrar a mercado la exposición actual de BTC. El gráfico recibe velas de BTC, Highest(24), Lowest(24), el punto medio, P&L, órdenes enviadas y todas las ejecuciones de la estrategia.

## Uso

Importe el archivo `.json` en Designer, establezca Strategy Security en BTCUSDT@BNBFT, ejecútelo en el backtester con historial de velas y profundidad de mercado, y después ajuste los parámetros o bloques a su instrumento antes de operar en vivo.
