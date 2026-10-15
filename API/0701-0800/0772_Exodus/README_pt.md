# Estratégia Exodus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma versão simplificada do script TradingView **EXODUS**. Utiliza um oscilador de momentum ponderado por volume (VWMO) junto com o Average Directional Index para detectar movimentos direcionais fortes.

## Detalhes

- **Critérios de entrada**
  - Comprado: `VWMO > VwmoThreshold` e `ADX > AdxThreshold`.
  - Vendido: `VWMO < -VwmoThreshold` e `ADX > AdxThreshold`.
- **Critérios de saída**
  - O momentum cruza zero ou um sinal oposto aparece.
- **Indicadores**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **Parâmetros**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`
