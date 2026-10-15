# Estrategia Clásica de Arbitraje Z-Score Desnudo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera el diferencial entre dos activos usando el Z-Score. Cuando el z-score del diferencial sube por encima de un umbral positivo, la estrategia vende el primer activo y compra el segundo. Cuando el z-score cae por debajo del umbral negativo, compra el primer activo y vende el segundo. Las posiciones se cierran cuando el z-score revierte hacia cero.

## Parámetros
- Tipo de vela
- Período de lookback
- Umbral Z-Score
- Segundo instrumento
