# Bollinger Divergence
[Русский](README_ru.md) | [中文](README_cn.md)

Bollinger Divergence hunts for extremes where price pierces a band yet the
opposite band begins to contract.  This divergence between price momentum and
volatility often precedes a snap back toward the middle of the range.

A long signal appears when a candle closes beneath the lower band while the
upper band narrows by at least a set percentage.  For shorts the pattern is
mirrored around the upper band.  Positions target a quick move back to the
middle Bollinger line with an optional fixed take‑profit.

The setup performs best in range‑bound markets or after a volatility spike
begins to fade.  The `CandlePercent` parameter controls how much the opposite
band must contract before a trade is allowed, helping avoid whipsaws during
strong trends.

## Details
- **Data**: Price candles.
- **Entry Criteria**:
  - **Long**: Close below lower band AND upper band contracts by `CandlePercent`.
  - **Short**: Close above upper band AND lower band contracts by `CandlePercent`.
- **Exit Criteria**:
  - Return to middle band OR take profit percentage.
- **Stops**: No hard stop; relies on take profit or manual exit.
- **Default Values**:
  - `BBLength` = 20
  - `BBMultiplier` = 2.0
  - `CandlePercent` = 30
  - `TakeProfit` = 5
- **Filters**:
  - Category: Mean reversion
  - Direction: Long & Short
  - Indicators: Bollinger Bands
  - Complexity: Simple
  - Risk level: Medium
