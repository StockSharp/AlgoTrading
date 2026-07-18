# Estrategia pendiente de DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia StockSharp reproduce el comportamiento del asesor experto MetaTrader "DeMarker Pending 2.5". El robot evalúa el oscilador DeMarker en un período de tiempo configurable y, cuando se cruzan niveles extremos, coloca una orden pendiente en la dirección de ruptura. La orden puede ser una orden stop o limitada compensada por un número fijo de puntos. El filtrado opcional de la ventana comercial y el vencimiento automático mantienen las órdenes pendientes alineadas con el comportamiento del experto original.

## Lógica comercial
- Suscríbase a la serie de velas seleccionada y calcule el indicador DeMarker con período `DemarkerPeriod`.
- Detecte cruces de los umbrales inferior (`DemarkerLowerLevel`) y superior (`DemarkerUpperLevel`) utilizando los valores de vela finalizados actuales y anteriores.
- Cuando el nivel inferior se cruza hacia arriba, se hace una cola larga; cuando el nivel superior se cruza hacia abajo, haga cola para una configuración breve.
- Convierta configuraciones en órdenes pendientes al precio `Close ± PendingIndentPoints * PriceStep`, usando órdenes stop en modo de ruptura o órdenes limitadas para entradas de retroceso dependiendo de `Mode`.
- Adjunte niveles de stop-loss y take-profit a la orden pendiente compensando el precio de entrada en `StopLossPoints` y `TakeProfitPoints` puntos.
- Cancele o reutilice pedidos pendientes anteriores de acuerdo con `ReplacePreviousPending` y `SinglePendingOnly` antes de registrar uno nuevo.
- Elimine los pedidos pendientes automáticamente una vez que transcurra su `PendingExpirationMinutes` vida útil.
- Ignore las señales fuera de la ventana intradiaria cuando `UseTimeWindow` esté habilitado. Cada barra se procesa solo una vez, por lo que se crea como máximo una nueva orden pendiente por barra y dirección.

## Gestión de pedidos
- Todas las entradas se crean como órdenes pendientes (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`).
- Cada orden pendiente tiene sus propios precios de límite de pérdidas y obtención de ganancias para que la posición esté protegida inmediatamente después de la activación.
- Las órdenes pendientes se cancelan al vencimiento, cuando se reemplazan por nuevas configuraciones o cuando el estado de la orden cambia a un estado inactivo (completado, cancelado, rechazado).

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Volumen de pedidos en lotes. |
| `StopLossPoints` | Distancia entre el precio de entrada y el stop-loss en puntos. |
| `TakeProfitPoints` | Distancia entre el precio de entrada y el take-profit en puntos. |
| `PendingIndentPoints` | Compensación entre el precio de mercado y la orden pendiente. |
| `PendingExpirationMinutes` | Duración de cada orden pendiente en minutos (0 desactiva el vencimiento). |
| `Mode` | Tipo de orden pendiente (parada para rupturas o límite para retrocesos). |
| `SinglePendingOnly` | Si está habilitado, evita realizar más de una orden pendiente activa. |
| `ReplacePreviousPending` | Cancela las órdenes pendientes activas antes de emitir una nueva. |
| `DemarkerPeriod` | Período retrospectivo del oscilador DeMarker. |
| `DemarkerUpperLevel` | Umbral de DeMarker que desencadena configuraciones de venta. |
| `DemarkerLowerLevel` | Umbral de DeMarker que desencadena configuraciones de compra. |
| `CandleType` | Plazo utilizado para la suscripción de velas y la evaluación de indicadores. |
| `UseTimeWindow` | Habilita el filtrado horario intradiario. |
| `StartTime` | Inicio de la ventana de negociación intradiaria. |
| `EndTime` | Fin de la ventana de negociación intradiaria. |

## Notas
- El experto original incluye sofisticadas rutinas de gestión del dinero y trailing-stop. Este puerto mantiene la generación de señales y el manejo de órdenes pendientes, pero simplifica el tamaño de la posición a un único parámetro fijo `Volume`.
- StockSharp adjunta precios de límite de pérdidas y obtención de ganancias en el momento del registro de la orden; Dependiendo del corredor, es posible que deba verificar que las órdenes de parada y límite respalden esos niveles de protección.
- Asegúrese siempre de que las distancias basadas en puntos sean compatibles con el `PriceStep` del símbolo negociado. Establezca `PendingIndentPoints`, `StopLossPoints` y `TakeProfitPoints` en valores que satisfagan los requisitos de distancia mínima del corredor.
