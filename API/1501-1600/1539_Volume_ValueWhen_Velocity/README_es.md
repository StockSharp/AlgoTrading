# Estrategia de Velocidad de Volumen ValueWhen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca entradas largas cuando el volumen se expande, el mercado está sobrevendido según el RSI, la volatilidad medida por el ATR se contrae y la distancia entre las rupturas recientes de la SMA supera un valor especificado. Cuando se cumplen todas las condiciones, se emite una orden de compra a mercado.

## Parámetros
- **RSI Length** – período para el RSI.
- **RSI Oversold** – umbral de sobreventa.
- **ATR Small / ATR Big** – períodos para la comparación de ATR.
- **Distance** – diferencia mínima entre precios de ruptura.
- **Candle Type** – marco temporal de las velas de entrada.
