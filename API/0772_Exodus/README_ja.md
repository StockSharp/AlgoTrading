# Exodus戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はTradingViewの**EXODUS**スクリプトを簡略化して移植したものです。出来高加重モメンタムオシレーター（VWMO）とAverage Directional Indexを組み合わせて、強い方向性のある動きを検出します。

## 詳細

- **エントリー条件**
  - ロング: `VWMO > VwmoThreshold` かつ `ADX > AdxThreshold`。
  - ショート: `VWMO < -VwmoThreshold` かつ `ADX > AdxThreshold`。
- **エグジット条件**
  - モメンタムがゼロを横切るか、反対のシグナルが現れる。
- **インジケーター**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **パラメーター**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`
