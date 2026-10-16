# Exodus Strategy
[Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

This strategy is a simplified port of the TradingView **EXODUS** script. It uses a volume weighted momentum oscillator (VWMO) together with the Average Directional Index to detect strong directional moves.

## Details

- **Entry Criteria**
  - Long: `VWMO > VwmoThreshold` and `ADX > AdxThreshold`.
  - Short: `VWMO < -VwmoThreshold` and `ADX > AdxThreshold`.
- **Exit Criteria**
  - Momentum crosses zero or an opposite signal appears.
- **Indicators**
  - Average True Range
  - Average Directional Index
  - Simple Moving Average
- **Parameters**
  - `VwmoMomentum`, `VwmoVolume`, `VwmoSmooth`, `VwmoThreshold`
  - `AtrLength`, `AtrMultiplier`, `TpMultiplier`
  - `AdxLength`, `AdxThreshold`
  - `Volume`
  - `CandleType`

**Precise simplified VWMO contract and defaults:** `VwmoMomentum=10`, `VwmoVolume=20`, `VwmoSmooth=3`, `VwmoThreshold=25`, `AdxLength=14`, `AdxThreshold=25`, `AtrLength=14`, `AtrMultiplier=2`, `TpMultiplier=3`, `CandleType=1 minute`. For each completed bar `i`, let `d_i=Close_i−Close_(i−VwmoMomentum)`. Raw `VWMO=100 × Σ(d_i × Volume_i) / Σ(|d_i| × Volume_i)` over the last `VwmoVolume` bars (zero if denominator is zero); smooth it by an SMA over the last `VwmoSmooth` raw values. It is a normalized **signed volume-weighted momentum** (−100..100), not an EMA crossover. After the VWMO and true ADX are formed, a long signal requires `VWMO > +VwmoThreshold && ADX > AdxThreshold`; a short requires `VWMO < −VwmoThreshold && ADX > AdxThreshold`. An opposite signal reverses the whole existing position in one market order. Without an opposite signal, close a long as VWMO crosses from positive to nonpositive, or a short from negative to nonnegative. Initial protection uses a Wilder ATR (`AtrLength`), native absolute stop distance `ATR × AtrMultiplier`, and take-profit distance `stop × TpMultiplier`. Native market protection observes Level1 bid and ask changes between completed candles. Orders remain blocked while a prior order is active; future data are never used.
