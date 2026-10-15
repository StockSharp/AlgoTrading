# Estrategia de cuadrícula Turbo Scaler
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Turbo Scaler Grid es una implementación StockSharp de alto nivel del asesor experto MQL5 "Turbo Scaler Grid Pending". La estrategia se centra en gestionar las rejillas de parada pendientes en torno a niveles de precios predefinidos, proteger dinámicamente las posiciones abiertas con lógica de equilibrio y seguimiento, y supervisar el capital de la cuenta para cerrar posiciones cuando se alcanzan los umbrales de ganancias o pérdidas.

La lógica funciona en múltiples períodos de tiempo simultáneamente:

- Un período de tiempo de activación configurable observa las señales de proximidad de precios que activan la cuadrícula pendiente.
- Las velas adicionales de 30 minutos, 2 horas y diarias brindan confirmación para activadores condicionales opcionales.
- Los datos de Level1 proporcionan los últimos valores de oferta/demanda utilizados para posicionar órdenes pendientes y gestionar los trailingstops.

## Reglas de trading
1. **Cuadrícula pendiente**
   - Las órdenes de compra y venta se colocan a partir de precios ancla configurables (`BuyStopEntry` y `SellStopEntry`).
   - Los pedidos están espaciados por `PendingStepPoints` y limitados por `PendingQuantity`.
   - El precio de activación verifica las velas recientes en el período de activación para confirmar que el precio se acercó al nivel de anclaje con suficiente impulso.
   - El activador de condición valida filtros adicionales de múltiples períodos de tiempo (rangos de bloques diarios, dirección de velas H2 y M30 y nivel de rango medio) antes de colocar órdenes pendientes.
2. **Protección de posición**
   - El stop loss inicial se calcula a partir de `StopLossPoints` (o anulaciones de precio fijo).
   - Cuando el precio avanza `BreakevenTriggerPoints`, el stop se mueve al precio de entrada más `BreakevenOffsetPoints` (para posiciones largas) o menos la compensación (para posiciones cortas).
   - Un trailing stop se activa solo después de alcanzar el punto de equilibrio y se actualiza una vez que el precio supera el stop anterior en `TrailMultiplier * TrailPoints`.
3. **Supervisión de patrimonio**
   - La estrategia monitorea el PnL flotante y fuerza la liquidación de posiciones si la reducción excede `MaxFloatLoss` (escalado al volumen de orden seleccionado).
   - Un activador de ganancias flotantes bloquea las ganancias al colocar una línea de capital interna en `EquityBreakeven` y seguirla en `EquityTrail` una vez que las ganancias superan `EquityTrigger`.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `StopLossPoints` | Distancia inicial de stop-loss en puntos. |
| `BreakevenTriggerPoints` | Puntos necesarios para activar el movimiento de equilibrio. |
| `BreakevenOffsetPoints` | Compensación agregada al precio de entrada cuando el stop se mueve al punto de equilibrio. |
| `TrailPoints` | Distancia utilizada para seguir después del punto de equilibrio. |
| `TrailMultiplier` | Multiplicador aplicado antes de que se establezca un nuevo trailing stop. |
| `BuyStopLossPrice` / `SellStopLossPrice` | Precios de parada fijos opcionales para posiciones largas/cortas. |
| `BuyStopEntry` / `SellStopEntry` | Precios base para las parrillas de parada pendientes. |
| `OrderVolume` | Volumen por orden pendiente. |
| `PendingQuantity` | Número máximo de órdenes pendientes activas. |
| `PendingStepPoints` | Distancia entre órdenes pendientes consecutivas. |
| `TriggerCandleType` | Serie de velas utilizada para la lógica de activación del precio. |
| `PendingPriceTrigger` | Habilita el disparador de proximidad de precio. |
| `PendingConditionTrigger` | Habilita el activador de confirmación de múltiples períodos de tiempo. |
| `OrderBuyBlockStart` / `OrderBuyBlockEnd` | Bloque bajo diario utilizado para validar configuraciones largas. |
| `OrderSellBlockStart` / `OrderSellBlockEnd` | Bloque alto diario utilizado para validar configuraciones cortas. |
| `MaxFloatLoss` | Pérdida flotante máxima permitida (escalada por volumen). |
| `EquityBreakeven` | Nivel de capital mantenido después de que se activa el activador de ganancias. |
| `EquityTrigger` | Se requiere beneficio flotante para crear el bloqueo de capital. |
| `EquityTrail` | Distancia de seguimiento aplicada al bloqueo de equidad. |

## Notas
- El volumen del pedido se escala para que coincida con el comportamiento original de EA (los lotes `0.01` se tratan como el paso base).
- Todos los comentarios dentro del código están escritos en inglés, mientras que este documento proporciona una descripción detallada para una rápida incorporación.
- La estrategia utiliza solo API StockSharp de alto nivel (`SubscribeCandles`, `Bind`, `BuyStop`, `SellStop`, `SellMarket`, `BuyMarket`) de acuerdo con los requisitos del proyecto.
