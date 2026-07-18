# Ejemplo de estrategia Trailingstop MT5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**SampleTrailingstopMt5Strategy** reproduce el comportamiento del MetaTrader5 asesor experto `SampleTrailingstop-MT5.mq5` utilizando el nivel alto de StockSharp API. La estrategia mantiene constantemente órdenes stop de ruptura emparejadas, protege las posiciones ocupadas con órdenes de salida dedicadas y aplica un stop dinámico una vez que la operación se vuelve rentable. Todos los cálculos se basan en el paso del precio del instrumento para que la lógica coincida con la implementación original basada en "puntos".

## Lógica comercial
1. **Fuente de datos**. La estrategia se suscribe a cotizaciones de nivel 1 para recibir los mejores precios de oferta y demanda que impulsan la orden y las actualizaciones del trailing stop.
2. **Pedidos de entrada**.
   - Se coloca una orden stop de compra por encima del mercado actual usando `BuyStop`. El pedido se actualiza solo cuando se completa la instancia anterior.
   - Una orden de venta stop refleja la entrada larga utilizando `SellStop` por debajo del mercado.
   - Ambas órdenes de entrada comparten el mismo volumen configurable, distancias de stop-loss y take-profit. Los pedidos también reciben una fecha de vencimiento con un día de anticipación, que coincide con la implementación de MQL.
3. **Protección de posición**.
   - Después de la ejecución, la estrategia rastrea la posición neta firmada y el precio de entrada promedio.
   - Se crean órdenes de salida y toma de ganancias separadas (`SellStop`/`BuyStop` y `SellLimit`/`BuyLimit`) para que los niveles de protección permanezcan en el intercambio incluso si las órdenes de entrada se cancelan o vencen.
   - Las órdenes de salida se sincronizan continuamente con el tamaño de la posición actual y el precio de entrada promedio más reciente.
4. **Lógica final**.
   - Cuando el beneficio flotante alcanza la distancia final configurada, el stop protector se ajusta para mantener esa distancia de la oferta actual (para largos) o demanda (para cortos).
   - El trailing stop nunca cruza el precio de entrada y respeta un incremento mínimo de actualización igual a un paso de precio.
5. **Seguimiento de posición**. Cada operación propia actualiza el valor de la posición acumulada y recalcula el precio de entrada promedio ponderado para que las ejecuciones y reversiones parciales se procesen correctamente.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Volumen de orden fijo (lotes o contratos) utilizado para ambas órdenes stop de ruptura. |
| `TakeProfitPoints` | Distancia en puntos del instrumento para el objetivo de ganancias. Establezca en cero para desactivar la obtención de beneficios. |
| `StopLossPoints` | Distancia en puntos para el stop-loss protector. |
| `TrailingStopPoints` | Distancia de seguimiento en puntos aplicada una vez que la posición es rentable. Zero desactiva el seguimiento. |

## Notas de comportamiento
- Las órdenes de inscripción solo se vuelven a enviar después de que finaliza la instancia anterior (completada, cancelada o vencida). Esto refleja la lógica `CheckPendingOrder` del experto original.
- Las distancias de stop-loss y take-profit siempre se convierten en valores de precio utilizando `Security.PriceStep`, lo que garantiza un comportamiento consistente entre diferentes instrumentos.
- Si la posición está completamente cerrada, la estrategia cancela automáticamente todas las órdenes de salida restantes y restablece los promedios internos.
- La estrategia se basa únicamente en datos de nivel 1 y no requiere velas ni indicadores, lo que mantiene la conversión cerca de la plantilla MQL.

## Uso
1. Asigne el valor y la cartera deseados antes de iniciar la estrategia.
2. Ajuste los cuatro parámetros públicos para alinearlos con el instrumento negociado (volumen, stop-loss, take-profit y distancia de seguimiento).
3. Lanzar la estrategia. Gestionará de forma autónoma las órdenes de ruptura y la protección de posiciones en tiempo real.
