# Estrategia Simple FX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia usa dos medias móviles exponenciales para detectar cambios de tendencia. Se abre una posición larga cuando la EMA corta cruza por encima de la EMA larga, mientras que se abre una posición corta cuando la EMA corta cruza por debajo de la EMA larga.

## Parámetros
- **Long MA Period** – período de la EMA larga.
- **Short MA Period** – período de la EMA corta.
- **Stop Loss (points)** – stop de protección en pasos de precio.
- **Take Profit (points)** – objetivo de ganancia en pasos de precio.
- **Candle Type** – marco temporal para las velas.
