# Estrategia de Operación en Hora de Noticias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **News Hour Trade** coloca órdenes stop de compra y venta pendientes alrededor de eventos de noticias de alto impacto programados. Las órdenes se desplazan del precio actual un número fijo de pasos e incluyen gestión de stop-loss, take-profit y trailing stop opcional.

## Idea

1. En la hora y minuto de inicio configurados, la estrategia se prepara para un próximo comunicado de noticias.
2. Se colocan una orden buy stop y una sell stop a `PriceGap` pasos por encima y por debajo del precio actual.
3. Cuando se activa una orden, la orden pendiente opuesta se cancela automáticamente.
4. La posición abierta está protegida con niveles fijos de stop-loss y take-profit. Si `TrailStop` está habilitado, el nivel de stop sigue al precio cuando se mueve a favor de la posición.
5. Solo se permite una operación por día.

## Parámetros

- **StartHour / StartMinute** – hora de inicio del trading.
- **DelaySeconds** – pausa antes de colocar las órdenes (actualmente informativo).
- **Volume** – tamaño de la orden en lotes.
- **StopLoss** – distancia al stop-loss en pasos de precio.
- **TakeProfit** – distancia al take-profit en pasos.
- **PriceGap** – desplazamiento desde el precio actual para las órdenes pendientes.
- **Expiration** – tiempo de vida de la orden pendiente en segundos (0 significa sin expiración).
- **TrailStop** – habilitar trailing stop.
- **TrailingStop** – distancia desde el precio actual para el trailing stop.
- **TrailingGap** – brecha mínima antes de actualizar el trailing stop.
- **BuyTrade / SellTrade** – habilitar órdenes de compra o venta.
- **CandleType** – marco temporal utilizado para el seguimiento del tiempo.

## Notas

La estrategia está pensada para el marco temporal M5, pero puede aplicarse a cualquier instrumento con spreads bajos. Utilícela con precaución durante eventos de noticias importantes.
