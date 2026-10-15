# Estrategia MultiCapa Awesome Oscillator Saucer
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa una estrategia alcista de múltiples capas basada en el patrón saucer del Awesome Oscillator y la detección de tendencia por fractales. La estrategia cuenta señales saucer consecutivas y coloca hasta cinco órdenes de compra stop escalonadas por encima del precio. Las posiciones se cierran cuando la tendencia se revierte.

## Parámetros
- **EMA Length** – período del filtro EMA.
- **Candle Type** – tipo de velas.
- **Trade Start** – inicio del período de trading.
- **Trade Stop** – fin del período de trading.
