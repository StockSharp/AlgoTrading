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

**簡略VWMOの厳密な式と初期値:** `VwmoMomentum=10`、`VwmoVolume=20`、`VwmoSmooth=3`、`VwmoThreshold=25`、`AdxLength=14`、`AdxThreshold=25`、`AtrLength=14`、`AtrMultiplier=2`、`TpMultiplier=3`、`CandleType=1分`。確定足`i`について`d_i=Close_i−Close_(i−VwmoMomentum)`、直近`VwmoVolume`本の生VWMOは`100 × Σ(d_i×Volume_i)/Σ(|d_i|×Volume_i)`（分母0なら0）。`VwmoSmooth`本のSMAで平滑化。これは−100..100の**符号付き出来高加重モメンタム**でありEMAクロスではありません。ロングは`VWMO > +VwmoThreshold && ADX > AdxThreshold`、ショートは`VWMO < −VwmoThreshold && ADX > AdxThreshold`。逆シグナルで成行1注文により全量反転。逆シグナルがなければVWMOのゼロクロスで決済。ネイティブストップ幅=`Wilder ATR(AtrLength) × AtrMultiplier`、利確幅=`ストップ幅 × TpMultiplier`、確定足の間もLevel1 bid/askで監視します。未完了注文がある間の重複注文や未来参照はありません。
