# Estrategia Exodus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación simplificada del script TradingView **EXODUS**. Utiliza un oscilador de momentum ponderado por volumen (VWMO) junto con el Average Directional Index para detectar movimientos direccionales fuertes.

## Detalles

- **Criterios de entrada**
  - Largo: `VWMO > VwmoThreshold` y `ADX > AdxThreshold`.
  - Corto: `VWMO < -VwmoThreshold` y `ADX > AdxThreshold`.
- **Criterios de salida**
  - El momentum cruza cero o aparece una señal opuesta.
- **Indicadores**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **Parámetros**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`
