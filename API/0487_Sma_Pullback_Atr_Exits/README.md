# SMA Pullback + ATR Exits Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy enters on pullbacks when a short-term moving average is above or below
a longer-term trend average. Long positions are opened when price dips below the fast SMA while it remains above the slow SMA. Short positions are opened when price rallies above the fast SMA while it stays below the slow SMA. Exits use Average True Range multiples from the entry price.

## Details

- **Entry Criteria**:
  - Long: close < fast SMA and fast SMA > slow SMA.
  - Short: close > fast SMA and fast SMA < slow SMA.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Price reaches ATR-based stop loss or take profit.
- **Stops**: ATR multiples for stop loss and take profit.
- **Default Values**:
  - `FastSmaLength` = 8
  - `SlowSmaLength` = 30
  - `AtrLength` = 14
  - `AtrMultiplierSl` = 1.2
  - `AtrMultiplierTp` = 2.0
- **Filters**:
  - Category: Trend following
  - Direction: Both
  - Indicators: SMA, ATR
  - Stops: Yes
  - Complexity: Low
  - Timeframe: Any
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
