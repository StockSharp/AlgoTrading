# Estrategia de Órdenes Pendientes por Tiempo 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el comportamiento del experto original de MetaTrader "Pending orders by time 2" programando órdenes de entrada de estilo ruptura alrededor de una hora de apertura configurable. Al inicio de la sesión de trading el algoritmo coloca tanto un buy stop por encima del precio ask actual como un sell stop por debajo del precio bid actual. Cada entrada pendiente lleva sus propios niveles de stop-loss y take-profit expresados en pasos de precio del instrumento, y una vez que una entrada se ejecuta la estrategia mantiene la posición abierta con lógica de trailing stop y órdenes de salida mutuamente exclusivas. El código está diseñado para la API de alto nivel de StockSharp y usa tabulaciones para la indentación según lo requieren las directrices del proyecto.

## Flujo de la Sesión de Trading
1. **Reinicio diario** – En la primera vela completada de un nuevo día de trading la estrategia limpia los indicadores internos para que más tarde en la sesión pueda emitirse un nuevo par de órdenes pendientes.
2. **Colocación en la hora de apertura** – Cuando la hora de la vela coincide con la hora de apertura configurada, y las órdenes aún no se han colocado para el día actual, la estrategia calcula los precios de ruptura relativos a la última instantánea del mejor bid/ask (recurre al cierre de la vela si no hay cotizaciones disponibles) y envía tanto buy stop como sell stop.
3. **Gestión intradía** – Mientras la sesión está activa, la lógica traza el stop protector para cualquier posición abierta, mantiene la entrada pendiente opuesta activa (permitiendo una posible reversión), y espera a que el trailing stop, el take-profit fijo, o la orden de ruptura opuesta cierren la posición.
4. **Limpieza en la hora de cierre** – Tan pronto como la hora de la vela coincide con la hora de cierre configurada, la estrategia cancela cualquier orden de entrada pendiente aún activa y cierra la posición neta a mercado, asegurando que no se lleven operaciones de un día para otro.

## Detalles de Colocación de Órdenes
- **Distancia, stop-loss, take-profit** – Los parámetros `DistanceTicks`, `StopLossTicks` y `TakeProfitTicks` se interpretan en pasos de precio del instrumento (`Security.PriceStep`). El precio del buy stop es `bestAsk + DistanceTicks * step`, su stop-loss se coloca `StopLossTicks` por debajo del precio de entrada, y el take-profit es la misma distancia por encima. El sell stop refleja esta lógica en el lado corto.
- **Manejo de bid/ask** – La estrategia se suscribe al libro de órdenes y registra continuamente el mejor bid y ask. Si el libro de órdenes aún no ha proporcionado una cotización, se usa el precio de cierre de la vela terminada como respaldo seguro.
- **Referencias de órdenes** – Las referencias a las órdenes pendientes enviadas se almacenan para que el algoritmo pueda cancelarlas o volver a registrarlas cuando la sesión cambie o cuando la hora de cierre se active.

## Gestión de Posición y Riesgo
- **Órdenes protectoras** – Cuando una entrada pendiente se ejecuta (detectada en `OnOwnTradeReceived`), la estrategia registra inmediatamente una orden de stop protector y una orden de take-profit con el volumen de posición original. Las posiciones largas reciben un `SellStop` y `SellLimit`, mientras que las posiciones cortas reciben un `BuyStop` y `BuyLimit`. Solo un stop y un take-profit permanecen activos en cualquier momento dado; emitir nuevas órdenes protectoras automáticamente cancela el par anterior.
- **Trailing stop** – El trailing se controla mediante `TrailingStopTicks` (la distancia real del stop) y `TrailingStepTicks` (beneficio mínimo requerido antes de un ajuste). La lógica de trailing se activa una vez que el beneficio no realizado supera `TrailingStop + TrailingStep`. Recalcula un precio de stop mejor (nunca aflojando el stop actual), cancela la orden de stop protector anterior y envía una nueva en el nivel más ajustado.
- **Salida en hora de cierre** – Cuando llega la hora de cierre, la estrategia cancela ambas órdenes protectoras y envía una orden de mercado del tamaño de la posición absoluta para que no quede ninguna exposición abierta.

## Parámetros
- `OpeningHour` – Hora (0–23) en que se crean las órdenes pendientes.
- `ClosingHour` – Hora (0–23) en que se eliminan las órdenes pendientes y se cierran las posiciones.
- `DistanceTicks` – Distancia de ruptura desde el bid/ask actual expresada en pasos de precio.
- `StopLossTicks` – Distancia protectora fija para el stop inicial.
- `TakeProfitTicks` – Distancia fija para el objetivo de beneficio.
- `TrailingStopTicks` – Distancia mantenida por el trailing stop una vez activado.
- `TrailingStepTicks` – Beneficio adicional mínimo requerido antes de que el trailing stop se mueva nuevamente.
- `Volume` – Tamaño de ambas órdenes pendientes.
- `CandleType` – Marco temporal utilizado para el seguimiento de sesiones y evaluación de señales (por defecto marco temporal de 15 minutos).

## Notas de Implementación
- Usa la API `Strategy` de alto nivel de StockSharp con enlaces `SubscribeCandles` y `SubscribeOrderBook`; no se requiere acceso de indicadores de bajo nivel.
- `OnOwnTradeReceived` se aprovecha para mantener las órdenes protectoras sincronizadas con la orden de entrada ejecutada y para limpiar cuando el stop-loss o take-profit se ejecuta.
- La lógica de trailing deliberadamente evita llamar a `GetValue` del indicador y se basa solo en la vela entrante y el estado almacenado, cumpliendo con las directrices de conversión.
- Las distancias se basan en pasos de precio, reflejando la aritmética original basada en pips de la implementación MQL y permaneciendo independiente del instrumento.
- La implementación en Python es intencionalmente omitida según los requisitos de la tarea; solo se proporciona la versión en C# en esta carpeta.
