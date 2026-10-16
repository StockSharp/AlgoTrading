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

**Fórmula VWMO simplificada e valores padrão:** `VwmoMomentum=10`, `VwmoVolume=20`, `VwmoSmooth=3`, `VwmoThreshold=25`, `AdxLength=14`, `AdxThreshold=25`, `AtrLength=14`, `AtrMultiplier=2`, `TpMultiplier=3`, `CandleType=1 minuto`. Em cada candle fechado `i`, `d_i=Close_i−Close_(i−VwmoMomentum)`. VWMO bruto `=100 × Σ(d_i×Volume_i)/Σ(|d_i|×Volume_i)` nos últimos `VwmoVolume` candles (zero para denominador zero), suavizado por SMA de `VwmoSmooth`. É impulso **com sinal ponderado por volume** (−100..100), não cruzamento EMA. Compra para `VWMO > +VwmoThreshold && ADX > AdxThreshold`; venda a descoberto para `VWMO < −VwmoThreshold && ADX > AdxThreshold`. Sinal contrário reverte a posição inteira numa ordem de mercado; sem sinal oposto, o cruzamento do VWMO pelo zero encerra a posição. Stop nativo `Wilder ATR(AtrLength) × AtrMultiplier`, alvo `Stop × TpMultiplier`, monitorando bid/ask Level1 entre candles. Sem ordens duplicadas ativas ou futuro.
