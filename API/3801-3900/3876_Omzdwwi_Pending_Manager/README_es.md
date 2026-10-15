# Estrategia de administrador pendiente de Omzdwwi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de administrador pendiente de Omzdwwi** es una traducción directa de alto nivel StockSharp del MetaTrader 4 experto `omzdwwi7739cyjayvs_1_65.mq4`. El asesor original se enfoca en mantener un anillo de órdenes pendientes alrededor del precio de mercado actual, ejecutar entradas de mercado en un temporizador programado y administrar paradas dinámicas tanto para posiciones activas como para órdenes pendientes pendientes. Esta versión de C# reproduce la misma lógica al tiempo que aprovecha el feed `Strategy` API, `SubscribeLevel1` de StockSharp y los asistentes de administración de pedidos (`BuyStop`, `SellLimit`, `ReRegisterOrder`, etc.).

La estrategia continuamente:

- Mantiene hasta cuatro órdenes pendientes (stop de compra, stop de venta, límite de compra, límite de venta) a distancias configurables de las cotizaciones de oferta y demanda.
- Opcionalmente, dispara órdenes de compra/venta de mercado a una hora y minuto específicos.
- Aplica múltiples capas de salidas para posiciones de mercado: toma de ganancias fija, stop-loss fijo, objetivo de "beneficio de pips" adicional y lógica de stop dinámico que imita la rutina `TrailingPositions()` del experto.
- Acerca o aleja las órdenes pendientes del precio según las reglas `TrailingOtlozh()` del experto una vez que el mercado avanza la distancia de seguimiento configurada.
- Supervisa los umbrales de pérdidas y ganancias a nivel de cuenta y emite registros de información/advertencia cuando se alcanzan los porcentajes globales configurados de obtención de beneficios o límite de pérdidas.

## Flujo de señal y suscripciones de datos.

- `SubscribeLevel1()` ofrece actualizaciones de oferta/demanda. Cada actualización de cotización activa verificaciones de tiempo, colocación de pedidos, ajustes finales y verificaciones de salida. No se requieren datos de velas ni indicadores.
- `GetWorkingSecurities()` declara la suscripción de nivel 1 para que la estrategia pueda ejecutarse tanto en entornos reales como de backtesting.

## Lógica de entrada

1. **Órdenes de mercado programadas.** Cuando `UseTimeSignals` está habilitado y el reloj del servidor llega a `SignalHour:SignalMinute`, la estrategia genera pestillos booleanos derivados de los parámetros `Time*Signal`. La siguiente actualización de nivel 1 llama a `BuyMarket()` o `SellMarket()` siempre que `WaitClose`/`MaxMarketOrders` lo permita. Los pestillos se reinician inmediatamente después del intercambio.
2. **Órdenes pendientes persistentes.** Para cada tipo de orden habilitada (`EnableBuyStop`, `EnableSellStop`, `EnableBuyLimit`, `EnableSellLimit`) la estrategia verifica que haya una orden activa. Las órdenes ausentes se realizan en `Distance * PriceStep` puntos desde la mejor oferta/demanda, replicando el comportamiento `UstanOtlozh()` del experto. Si la orden ya existe, `ReRegisterOrder` mantiene el precio alineado con las cotizaciones actuales.

## Lógica de salida para posiciones de mercado

- **El stop-loss/take-profit fijo** proviene de `MarketStopLossPoints` y `MarketTakeProfitPoints`. Cuando la mejor oferta/demanda cruza esos umbrales, la posición se nivela mediante orden de mercado.
- **Objetivo de pips adicionales** replica el comportamiento `PipsProfit` del experto. Cuando es distinto de cero, cierra la posición después de obtener la ganancia configurada incluso si TP está deshabilitado.
- **Parada dinámica** copias `TrailingPositions()`. Una vez que la posición es suficientemente rentable (o inmediatamente si `RequireProfitBeforeTrailing=false`), el precio de seguimiento interno se actualiza a `Bid - MarketTrailingOffsetPoints * PriceStep` para largos y a `Ask + MarketTrailingOffsetPoints * PriceStep` para cortos con el paso mínimo de seguimiento impuesto por `MarketTrailingStepPoints`.

## Lógica final para órdenes pendientes

- Las órdenes de parada utilizan `StopTrailingOffsetPoints` y `StopTrailingStepPoints`. Cuando el precio cruza el umbral MQL (`Ask < OrderPrice - (offset + step)` para paradas de compra, simétrico para ventas), la orden se vuelve a registrar en `Ask + offset` o `Bid - offset`.
- Las órdenes limitadas utilizan `LimitTrailingOffsetPoints` y `LimitTrailingStepPoints` de la misma manera, recreando los ajustes de `TrailingOtlozh()`.

## Monitoreo de riesgos y cuentas

- `MaxMarketOrders` limita cuántos lotes (expresados en múltiplos de `OrderVolume`) se pueden acumular por dirección cuando `WaitClose=false`.
- `UseGlobalLevels`, `GlobalTakeProfitPercent` y `GlobalStopLossPercent` vigilan el capital de la cartera. Cuando se exceden los umbrales, la estrategia escribe un registro de información o advertencia, reflejando las ventanas emergentes de alerta originales.

## Parámetros

| grupo | Parámetro | Descripción |
|-------|-----------|-------------|
| generales | `OrderVolume` | Volumen comercial (lotes) reutilizado por cada pedido. |
| Ejecución | `WaitClose` | Bloquee nuevas entradas hasta que la posición neta sea plana. |
| Ejecución | `MaxMarketOrders` | Máximo de lotes simultáneos por dirección cuando se permite la formación piramidal. |
| Órdenes pendientes | `EnableBuyStop` / `EnableSellStop` / `EnableBuyLimit` / `EnableSellLimit` | Habilite o deshabilite cada tipo de orden pendiente. |
| Órdenes pendientes | `StopStepPoints`, `LimitStepPoints` | Distancia en puntos utilizados para colocar órdenes stop/limit en relación con la oferta/demanda actual. |
| Órdenes pendientes | `StopTakeProfitPoints`, `StopStopLossPoints`, `LimitTakeProfitPoints`, `LimitStopLossPoints` | Se aplican distancias de protección una vez que se activan las órdenes pendientes. |
| Órdenes pendientes | `StopTrailingOffsetPoints`, `StopTrailingStepPoints`, `LimitTrailingOffsetPoints`, `LimitTrailingStepPoints` | Parámetros de seguimiento para órdenes pendientes pendientes. |
| Riesgo de mercado | `MarketTakeProfitPoints`, `MarketStopLossPoints` | Take-profit y stop-loss en puntos para posiciones de mercado. |
| Riesgo de mercado | `MarketTrailingOffsetPoints`, `MarketTrailingStepPoints`, `RequireProfitBeforeTrailing` | Configuración de trailing stop para posiciones de mercado. |
| Riesgo de mercado | `ExitProfitPoints` | Objetivo de beneficio fijo adicional. |
| Gestión del tiempo | `UseTimeSignals`, `SignalHour`, `SignalMinute` | Configuración de ejecución programada. |
| Gestión del tiempo | `TimeBuySignal`, `TimeSellSignal`, `TimeBuyStopSignal`, `TimeSellStopSignal`, `TimeBuyLimitSignal`, `TimeSellLimitSignal` | Qué órdenes se activarán cuando se active el temporizador. |
| Monitoreo de cuenta | `UseGlobalLevels`, `GlobalTakeProfitPercent`, `GlobalStopLossPercent` | Umbrales de alerta a nivel de cartera. |
| Varios | `SlippagePoints` | Parámetro heredado reservado mantenido para que esté completo. |

## Notas de conversión

- El experto MQL estableció el take-profit/stop-loss directamente en las órdenes pendientes. StockSharp coloca la entrada pendiente primero y luego administra las salidas a través de la lógica estratégica para mantener la implementación dentro de las restricciones de alto nivel API.
- Se omitieron las alertas sonoras porque el registro StockSharp ya proporciona notificaciones estructuradas.
- La restricción `MODE_STOPLEVEL` de MetaTrader no existe en StockSharp; por lo tanto, los parámetros dependen de que el comerciante respete las distancias mínimas impuestas por el intercambio.
- El manejo de errores utiliza ventanas emergentes `AddInfoLog`/`AddWarningLog` en lugar de `Alert()`.

## Uso

1. Adjunte la estrategia a un `Security` y `Portfolio` con un paso de precio válido.
2. Configure distancias en puntos (se convierten automáticamente a unidades de precio usando el `ShrinkPrice` del valor).
3. Iniciar la estrategia; se suscribirá a cotizaciones de nivel 1 y comenzará a gestionar pedidos de inmediato.

> **Consejo:** Al realizar pruebas retrospectivas, asegúrese de que el evaluador proporcione datos de nivel 1 para que la lógica de seguimiento y sincronización reciba actualizaciones en cada cotización, tal como lo hizo el experto MQL original.
