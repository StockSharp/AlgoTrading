# VWAP EMA ATR Pullback
[Русский](README_ru.md) | [中文](README_cn.md)

Trend-following strategy using EMAs, VWAP, and ATR.

Testing indicates an average annual return of about 55%. It performs best in the futures market.

The approach identifies strong trends via fast and slow EMAs separated by an ATR-based distance. Entries occur when price pulls back to VWAP, aiming to join the trend. A take-profit is placed at the VWAP plus or minus the ATR multiple.

## Details

- **Entry Criteria**:
  - **Long**: uptrend and close < VWAP.
  - **Short**: downtrend and close > VWAP.
- **Long/Short**: Both sides.
- **Exit Criteria**: Target at VWAP ± ATR * multiplier.
- **Stops**: No.
- **Default Values**:
  - `FastEmaLength` = 30
  - `SlowEmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: EMA, ATR, VWAP
  - Stops: No
  - Complexity: Moderate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
