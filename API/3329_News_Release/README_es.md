# Estrategia News Release
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el comportamiento central del asesor experto original **NewsReleaseEA**, preparando un bracket de órdenes pendientes alrededor de una noticia programada y gestionando activamente la posición resultante.

## Ideas clave

- Cinco entradas (hora de noticia, ventanas antes/después, distancias de órdenes y separación) definen cuándo y dónde se colocan las órdenes stop.
- Un conjunto simétrico de órdenes buy stop y sell stop se envía poco antes de la hora configurada. El primer par se coloca a `DistancePips` del ask/bid actual y los pares adicionales se desplazan por `StepPips`.
- Las órdenes pendientes permanecen activas hasta `PostNewsMinutes` minutos después del evento. Al final de la ventana, la estrategia cancela todas las órdenes activas y, si se solicita, cierra cualquier posición abierta.
- Cuando una orden se ejecuta, las órdenes pendientes opuestas se cancelan automáticamente y la posición abierta se gestiona mediante reglas de stop-loss, take-profit, break-even y trailing expresadas en pips.
- La protección break-even se arma después de que el precio se mueve `BreakEvenTriggerPips` a favor de la posición y fuerza una salida si el precio vuelve al precio de entrada más `BreakEvenOffsetPips` (largos) o menos ese offset (cortos).
- La gestión trailing sigue el mejor precio alcanzado tras la entrada. Cuando la distancia entre el precio actual y el extremo supera `TrailingPips`, la posición se cierra para proteger el beneficio acumulado.
- La bandera `TradeOnce` replica el comportamiento "trade one time per news" del programa MQL impidiendo una segunda activación tras completarse la primera operación.

## Parámetros

- `NewsTime`: hora programada de la noticia.
- `PreNewsMinutes`: cuántos minutos antes de la noticia se colocan órdenes pendientes.
- `PostNewsMinutes`: cuántos minutos después de la noticia se mantienen vivas las órdenes pendientes antes de cancelarlas.
- `OrderPairs`: número de pares buy stop/sell stop que forman el bracket.
- `DistancePips`: distancia en pips del primer par respecto al mejor ask/bid en el momento de colocación.
- `StepPips`: separación adicional en pips entre pares consecutivos.
- `OrderVolume`: volumen enviado con cada orden pendiente.
- `TradeOnce`: si está activado, la estrategia solo puede operar una vez por ventana de evento.
- `UseStopLoss` / `StopLossPips`: activa y configura la distancia de stop-loss en pips.
- `UseTakeProfit` / `TakeProfitPips`: activa y configura la distancia de take-profit en pips.
- `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips`: configura el módulo break-even.
- `UseTrailing` / `TrailingPips`: activa la lógica de salida trailing y define la distancia en pips.
- `CloseAfterEvent`: cierra cualquier posición abierta al finalizar la ventana posterior a la noticia.

## Notas

- La estrategia trabaja exclusivamente con datos level1 (`SubscribeLevel1`) para reaccionar a los últimos precios bid/ask sin esperar velas.
- Las distancias expresadas en pips se convierten a precios absolutos usando `PriceStep` del instrumento. Si `PriceStep` no está disponible, se usa 1 como fallback seguro.
- Las condiciones de stop-loss, take-profit, break-even y trailing cierran la posición a mercado llamando a `ClosePosition()`. Esto replica la gestión reactiva del experto original manteniendo compacta la implementación.
- No se proporciona versión Python, tal como se solicitó.
