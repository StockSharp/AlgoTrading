# Estrategia STLMCandle
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en la dirección de la última vela completada.
Si el precio de cierre está por encima del precio de apertura, abre una posición larga y cierra cualquier posición corta.
Si el precio de cierre está por debajo del precio de apertura, abre una posición corta y cierra cualquier posición larga.
Soporta niveles de stop-loss y take-profit y opera en un marco temporal de velas configurable.

## Parámetros
- `CandleType` – marco temporal de las velas utilizadas para el análisis.
- `StopLoss` – valor absoluto de stop-loss en unidades de precio.
- `TakeProfit` – valor absoluto de take-profit en unidades de precio.

## Notas
La estrategia es una adaptación simplificada del asesor experto original MQL `STLMCandle`.
Aproxima el indicador usando los precios de apertura y cierre estándar de las velas.
