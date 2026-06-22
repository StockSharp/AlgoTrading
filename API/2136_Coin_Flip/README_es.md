# Estrategia de Lanzamiento de Moneda
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de Lanzamiento de Moneda** elige aleatoriamente ir largo o corto en cada nueva vela cuando no hay posición abierta. Después de cerrar una posición, si la operación terminó en pérdida, el tamaño de la siguiente operación se incrementa usando un multiplicador de martingala. La estrategia cierra posiciones usando niveles fijos de take-profit y stop-loss definidos en pasos de precio y opcionalmente puede seguir las ganancias después de una distancia especificada.

## Parámetros

- `Volume` – tamaño base de la orden usado para el primer intento.
- `Martingale` – multiplicador aplicado al volumen después de una operación perdedora.
- `MaxVolume` – límite superior del tamaño de posición después de incrementos por martingala.
- `TakeProfit` – objetivo de ganancia en pasos de precio.
- `StopLoss` – límite de pérdida en pasos de precio.
- `TrailingStart` – distancia en pasos de precio donde el trailing se activa.
- `TrailingStop` – distancia del trailing stop en pasos de precio.
- `CandleType` – marco temporal de las velas usado para la toma de decisiones.

## Cómo funciona

1. En cada vela completada, la estrategia verifica si hay una posición abierta.
2. Si existe una posición, monitorea la ganancia o pérdida usando el precio de cierre actual. Una vez que se cumplen las condiciones de take-profit, stop-loss o trailing stop, la posición se cierra.
3. Cuando no hay posición abierta, se lanza una moneda virtual:
   - Cara abre una posición larga.
   - Cruz abre una posición corta.
4. Si la operación anterior fue una pérdida, el volumen se multiplica por `Martingale` pero con un límite máximo de `MaxVolume`.
5. El trailing stop se activa una vez que el precio se mueve `TrailingStart` en la dirección favorable.

## Notas

Este ejemplo está destinado a fines educativos para demostrar cómo trabajar con señales aleatorias y dimensionamiento de posiciones usando la API de alto nivel de StockSharp.
