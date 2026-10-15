# Estrategia ColorX2MA Digit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port del experto MQL5 **Exp_ColorX2MA_Digit**.
El algoritmo original pinta una media móvil doblemente suavizada en diferentes colores según su pendiente y usa esos colores para generar señales de trading.
En esta versión en C# el comportamiento se aproxima mediante dos medias móviles simples y se opera en sus cruces.

## Lógica de trading

- Una media móvil **rápida** suaviza la serie de precios.
- Una media móvil **lenta** suaviza el resultado de la rápida.
- Cuando la media rápida cruza hacia arriba a la media lenta, la estrategia abre una posición larga y cierra cualquier posición corta existente.
- Cuando la media rápida cruza hacia abajo a la media lenta, la estrategia abre una posición corta y cierra cualquier posición larga existente.
- Las señales se procesan solo después de que la vela haya cerrado.

## Parámetros

- `FastLength` – longitud del primer suavizado (predeterminado 12).
- `SlowLength` – longitud del segundo suavizado (predeterminado 5).
- `CandleType` – marco temporal de las velas usadas para los cálculos.

La estrategia usa únicamente la API de alto nivel: `SubscribeCandles` con `Bind` para alimentar indicadores y `BuyMarket`/`SellMarket` para gestionar posiciones. Los comentarios en el código están en inglés para facilitar el mantenimiento.
