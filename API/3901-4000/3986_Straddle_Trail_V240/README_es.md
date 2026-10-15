# Estrategia Straddle Trail v2.40
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Straddle Trail v2.40** es una versión StockSharp del asesor experto MetaTrader 4 "Straddle&Trail" (versión 2.40). El algoritmo prepara un par simétrico de órdenes stop antes de un evento de alto impacto, gestiona automáticamente la posición activada con lógica de punto de equilibrio y trailing-stop, y puede reaccionar a las operaciones manuales que ya existen en la cuenta.

## Flujo de trabajo principal

1. **Preparación**
   - La estrategia se suscribe a actualizaciones del libro de pedidos para realizar un seguimiento de la mejor oferta/demanda y a velas diminutas (configurables) para tomar decisiones de programación.
   - Los pips se calculan a partir de la configuración del instrumento para que todas las distancias definidas en pips se conviertan correctamente en precios.
2. **Colocación a horcajadas**
   - En el tiempo de entrega configurado antes del evento (`PreEventEntryMinutes`), o inmediatamente si `PlaceStraddleImmediately` está habilitado, se colocan una orden de compra y venta a `DistanceFromPrice` pips por encima y por debajo del mercado.
   - Antes del evento, las órdenes pendientes se pueden volver a centrar cada minuto si `AdjustPendingOrders` está habilitado. Los ajustes se detienen `StopAdjustMinutes` antes del evento.
3. **Gestión de pedidos**
   - Una vez que se activa un lado, la eliminación opcional de la orden pendiente opuesta (`RemoveOppositeOrder`) evita la doble exposición.
   - `ShutdownNow` junto con `ShutdownOption` permite aplanar posiciones abiertas y/o cancelar órdenes pendientes bajo demanda.
4. **Protección de posición**
   - Los niveles iniciales de stop-loss y take-profit se derivan de los parámetros basados en pips.
   - Cuando el precio alcanza el punto de equilibrio, el stop se mueve para bloquear `BreakevenLockPips` de ganancia.
   - El seguimiento comienza inmediatamente o después del punto de equilibrio (dependiendo de `TrailAfterBreakeven`).
   - Si `ManageManualTrades` es verdadero, cualquier posición manual detectada por la estrategia se protegerá utilizando las mismas reglas.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `ShutdownNow` | Fuerza la lógica de apagado a ejecutarse en el siguiente cierre de vela. |
| `ShutdownOption` | Elige qué cerrar: todo, solo posiciones activadas, solo largas, solo cortas, todas las órdenes pendientes, solo paradas de compra o solo paradas de venta. |
| `DistanceFromPrice` | Distancia en pips entre el precio actual y las órdenes stop pendientes. |
| `StopLossPips` | Distancia inicial de stop-loss en pips. |
| `TakeProfitPips` | Distancia inicial de toma de ganancias en pips. Establezca en 0 para desactivar el nivel de obtención de beneficios. |
| `TrailPips` | Distancia del trailing-stop en pips. Establezca en 0 para desactivar el seguimiento. |
| `TrailAfterBreakeven` | Si es verdadero, el seguimiento solo comenzará después de que se alcance el punto de equilibrio. |
| `BreakevenLockPips` | Beneficio (en pips) bloqueado una vez que se activa el disparador del punto de equilibrio. |
| `BreakevenTriggerPips` | Umbral de beneficio (en pips) que activa el movimiento de equilibrio. |
| `EventHour` / `EventMinute` | Hora programada para el evento de noticias (hora del corredor). Establezca ambos en 0 para desactivar la programación y utilizar el modo manual/inmediato. |
| `PreEventEntryMinutes` | Minutos antes del evento cuando se coloca el straddle. |
| `StopAdjustMinutes` | Minutos antes del evento cuando cesan los ajustes de pedidos. El valor mínimo es 1 minuto. |
| `RemoveOppositeOrder` | Elimina la orden pendiente opuesta después de que se llena un lado del straddle. |
| `AdjustPendingOrders` | Vuelve a centrar las órdenes pendientes cada minuto hasta alcanzar la ventana de ajuste de parada. |
| `PlaceStraddleImmediately` | Coloca el straddle tan pronto como comienza la estrategia, ignorando el calendario del evento. |
| `ManageManualTrades` | Extiende la lógica de equilibrio y seguimiento a posiciones manuales. |
| `CandleType` | Serie de velas utilizada para la lógica de sincronización y programación (el valor predeterminado es un período de tiempo de 1 minuto). |

## Notas de uso

- Configure siempre el tamaño de pip correcto para el instrumento a través de la configuración de seguridad para que las distancias basadas en pips se traduzcan en precios con precisión.
- La estrategia cierra posiciones utilizando órdenes de mercado cuando se cumple una condición de límite de pérdidas o toma de ganancias, lo que refleja cómo el EA original realizó ajustes de límite manuales.
- Cuando `PlaceStraddleImmediately` está deshabilitado y el programa está activo, el straddle se coloca solo una vez por día de negociación. Restablezca la estrategia para prepararse para otro evento el mismo día.
- Los controles de apagado se pueden utilizar como freno de emergencia para reducir rápidamente la exposición y eliminar órdenes pendientes en todos los escenarios.

## Detalles de conversión

- Todos los comentarios del código se han traducido al inglés y se han ampliado con explicaciones adicionales para mayor claridad.
- Los métodos StockSharp API de alto nivel (`BuyStop`, `SellStop`, `ClosePosition`) se utilizan para mantener la implementación cerca de las mejores prácticas del marco.
- El algoritmo evita las búsquedas directas de indicadores y, en cambio, se basa en las velas vinculadas y las suscripciones al libro de órdenes, como lo exigen las pautas del proyecto.
