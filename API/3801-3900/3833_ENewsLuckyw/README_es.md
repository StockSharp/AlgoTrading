# ENoticiasEstrategia Luckyw
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia ENewsLuckyw** es un sistema de ruptura basado en el tiempo convertido del asesor experto MetaTrader *e-News-Lucky$*. A una hora programada, envía órdenes buy-stop y sell-stop alrededor del precio actual, las centra continuamente mientras ambas órdenes están activas y realiza una gestión de posiciones que imita la lógica MQL original. Las salidas protectoras, el seguimiento opcional y la limpieza al final del día completan el flujo de trabajo.

## Lógica de trading
- **Colocación combinada programada.** En `SetOrdersTime` la estrategia cancela cualquier orden pendiente restante, mide el cierre de la vela actual y coloca órdenes stop simétricas a `DistancePips` del precio de mercado.
- **Actualización continua de órdenes.** Cuando ambas órdenes pendientes están activas, se realinean en cada vela terminada, manteniendo el straddle centrado en el precio como lo hizo el experto original en cada nueva barra.
- **Preparación de entrada.** Los niveles de stop-loss y take-profit opcionales se calculan previamente para que puedan adjuntarse inmediatamente cuando se abre una posición. Las órdenes pendientes opuestas se eliminan tan pronto como aparece una posición.
- **Protección de seguimiento.** Si `UseTrailing` está habilitado, la orden de parada se mueve `TrailingStopPips` siempre que la posición haya avanzado al menos `TrailingStepPips`. Con `ProfitTrailing` activado, el seguimiento comienza solo después de que las ganancias exceden la distancia de seguimiento, replicando el interruptor "ProfitTrailing" de MQL.
- **Limpieza de sesión.** A las `DeleteOrdersTime` todas las órdenes pendientes se cancelan y cualquier posición abierta se cierra para evitar mantener el riesgo durante la noche.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Volume` | Volumen de órdenes en lotes utilizados para ambas órdenes stop. |
| `StopLossPips` | Distancia de parada protectora. Cero desactiva la parada. |
| `TakeProfitPips` | Distancia de toma de ganancias opcional. Cero desactiva el objetivo. |
| `DistancePips` | Compensación del precio actual para las órdenes stop de ruptura. |
| `UseTrailing` | Permite detener el seguimiento una vez que la posición está abierta. |
| `ProfitTrailing` | Requiere que el beneficio no realizado supere la distancia de seguimiento antes de mover el stop. |
| `TrailingStopPips` | Distancia entre el precio y el trailing stop. |
| `TrailingStepPips` | Se necesita una mejora mínima antes de que se actualice nuevamente el trailing stop. |
| `SetOrdersTime` | Hora del día en que se coloca la silla a horcajadas. |
| `DeleteOrdersTime` | Hora del día para cancelar órdenes y cerrar posiciones. |
| `CandleType` | Suscripción de vela utilizada para el seguimiento del tiempo y el mantenimiento de pedidos. |

## Notas de uso
1. Adjunte la estrategia al instrumento deseado y configure `CandleType` para que coincida con el tamaño de barra que desea usar para el mantenimiento (el valor predeterminado es velas de 1 minuto).
2. Establezca los parámetros de programación para alinearlos con su evento noticioso o sesión comercial.
3. Ajustar distancias y controles de riesgo según la volatilidad del instrumento. Para los símbolos de Forex, asegúrese de que el paso del precio esté configurado correctamente para que `StopLossPips`, `TakeProfitPips` y `DistancePips` se traduzcan en las compensaciones de precio esperadas.
4. El sistema de seguimiento utiliza órdenes stop y límite para las salidas. Si su lugar no admite estos tipos de órdenes, reemplácelas con salidas de mercado u órdenes simuladas antes de publicarlas.
5. La estrategia realiza un reinicio diario por fecha. Si lo ejecuta hasta la medianoche en la zona horaria del intercambio, asegúrese de que la sesión de negociación abarque un solo día de negociación.

## Notas de conversión
- La estrategia refleja el flujo de trabajo del experto MQL: colocación programada (`SetOrders`), mantenimiento por hora (`ModifyOrders`), eliminación de órdenes pendientes en conflicto (`DeleteOppositeOrders`), lógica de seguimiento (`TrailingPositions`) y limpieza al final del día.
- Los cálculos de precios con reconocimiento de diferencial del código MQL se aproximan utilizando el último cierre de vela porque StockSharp normaliza los precios con respecto al `PriceStep` del instrumento.
- Se omitieron todas las configuraciones de sonido, número de cuenta y color del guión original porque no tienen equivalente en el nivel alto de StockSharp API.
