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

**Fórmula VWMO simplificada y valores iniciales:** `VwmoMomentum=10`, `VwmoVolume=20`, `VwmoSmooth=3`, `VwmoThreshold=25`, `AdxLength=14`, `AdxThreshold=25`, `AtrLength=14`, `AtrMultiplier=2`, `TpMultiplier=3`, `CandleType=1 minuto`. Para cada vela cerrada `i`, `d_i=Close_i−Close_(i−VwmoMomentum)`; VWMO bruto `=100 × Σ(d_i×Volume_i)/Σ(|d_i|×Volume_i)` en las últimas `VwmoVolume` velas (cero si denominador nulo), suavizado con SMA de `VwmoSmooth`. Es impulso **firmado ponderado por volumen** (−100..100), no cruce EMA. Largo si `VWMO > +VwmoThreshold && ADX > AdxThreshold`; corto si `VWMO < −VwmoThreshold && ADX > AdxThreshold`. Señal opuesta revierte toda la posición con una orden de mercado; sin señal opuesta, el cruce de VWMO por cero cierra la posición. Stop nativo `Wilder ATR(AtrLength) × AtrMultiplier`, objetivo `Stop × TpMultiplier`, sobre bid/ask Level1 incluso entre velas. Sin órdenes duplicadas activas ni datos futuros.
