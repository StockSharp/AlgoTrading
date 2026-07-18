# Estrategia Intraday Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia busca puntos de giro intradía utilizando pendientes de medias móviles suavizadas y el Índice de Fuerza Relativa (RSI).
Se abre una posición larga cuando la pendiente de la media móvil de 10 períodos gira al alza tras un movimiento bajista, el RSI está por debajo de 70
y la vela anterior es alcista. Se abre una posición corta cuando la pendiente gira a la baja tras un movimiento alcista, el RSI está
por encima de 30 y la vela anterior es bajista.

Un filtro de Average True Range (ATR) bloquea nuevas entradas cuando la volatilidad es demasiado alta. Las posiciones abiertas están protegidas por un
stop trailing adaptativo que se mueve a favor de la operación y sale cuando el precio cruza el nivel del stop.

## Parámetros
- **RSI Period** – período del indicador RSI.
- **Trailing Stop** – distancia del stop trailing en unidades de precio.
- **ATR Threshold** – valor máximo de ATR permitido para operar.
- **Candle Type** – marco temporal de las velas utilizadas para el análisis.
