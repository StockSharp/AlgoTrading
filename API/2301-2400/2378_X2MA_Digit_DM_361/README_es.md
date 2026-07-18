# Estrategia X2MA Digit DM 361
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia combina dos medias móviles con el Índice de Dirección Promedio (ADX).
Se abre una posición larga cuando la media móvil rápida está por encima de la lenta y el índice direccional positivo (+DI) es mayor que el negativo (-DI).
Se abre una posición corta cuando la media móvil rápida está por debajo de la lenta y -DI es mayor que +DI.

La estrategia utiliza protecciones de stop-loss y take-profit basadas en porcentajes. Las velas para los cálculos se toman del marco temporal especificado.

## Parámetros
- **Fast MA Length** – longitud de la media móvil rápida.
- **Slow MA Length** – longitud de la media móvil lenta.
- **ADX Length** – período para el cálculo del Índice de Dirección Promedio.
- **Stop Loss %** – tamaño del stop-loss en porcentaje del precio de entrada.
- **Take Profit %** – tamaño del take-profit en porcentaje del precio de entrada.
- **Candle Type** – marco temporal de las velas utilizadas para el procesamiento.
