# Estrategia Charles 1.3.7
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca órdenes stop simétricas por encima y por debajo del precio actual y usa salidas trailing para capturar rupturas.

## Parámetros

- **Anchor** – distancia en pasos de precio para colocar las órdenes stop.
- **XFactor** – multiplicador para el volumen de la orden.
- **Trailing Stop** – distancia del trailing stop en pasos de precio.
- **Trailing Profit** – umbral de ganancia para salir de la posición.
- **Stop Loss** – stop loss fijo en pasos de precio (0 lo desactiva).
- **Volume** – volumen base de la orden.
- **Candle Type** – marco temporal de las velas procesadas.

## Lógica de Trading

1. Cuando no hay posición abierta, se cancelan las órdenes existentes y se colocan tanto un Buy Stop como un Sell Stop a `Anchor` pasos del cierre de la última vela.
2. Cuando se abre una posición, se cancela la orden stop opuesta. El precio de entrada se recuerda para los cálculos de salida.
3. Para una posición larga, si la ganancia alcanza `Trailing Profit` o el precio cae `Stop Loss`, la posición se cierra. Para una posición corta, la lógica es espejo.

La estrategia está diseñada como ejemplo de trading de ruptura con gestión de riesgo simple.
