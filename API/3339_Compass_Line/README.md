# Compass Line Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy replicates the CompassLine expert by merging two complementary filters:

* **Follow Line** &mdash; a Bollinger Bands breakout trail optionally shifted by ATR. When price closes outside the bands the trail is extended in the breakout direction and never retreats while the trend persists.
* **Compass** &mdash; a logistic transform of the median price relative to the highest high and lowest low over the moving-average window. The raw signal is double-smoothed (triangular averaging) to produce a stable bullish/bearish state.

A position is opened only when both filters agree on the trend. Optional time filtering and protective stops mirror the MQL logic.

## Details

- **Entry Criteria**:
  - Follow Line must point upward (recent close above the upper band) for longs or downward (recent close below the lower band) for shorts. ATR displacement can be toggled with `UseAtrFilter`.
  - Compass state (based on `CompassPeriod`) must be positive for longs or negative for shorts after the double smoothing phase.
  - Trading is executed only when the optional session filter (`UseTimeFilter` with `Session` in HHmm-HHmm) allows it.
- **Long/Short**: Both directions are supported.
- **Exit Criteria**:
  - `CloseMode = None` keeps the position until an opposite entry or protective stop occurs.
  - `CloseMode = BothIndicators` closes when both Follow Line and Compass reverse direction simultaneously.
  - `CloseMode = FollowLineOnly` exits when Follow Line flips against the position.
  - `CloseMode = CompassOnly` exits when Compass changes polarity.
- **Stops**: `TakeProfit` and `StopLoss` distances (in security steps) are applied after every entry when greater than zero.
- **Default Values**:
  - `FollowBbPeriod` = 21
  - `FollowBbDeviation` = 1
  - `FollowAtrPeriod` = 5
  - `UseAtrFilter` = false
  - `CompassPeriod` = 30 (smoothing length = round(CompassPeriod / 3))
  - `CloseMode` = None
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `TakeProfit` = 0
  - `StopLoss` = 0
  - `CandleType` = TimeSpan.FromMinutes(15)
- **Filters**:
  - Category: Trend
  - Direction: Both
  - Indicators: Bollinger Bands, ATR, Triangular moving average
  - Stops: Optional
  - Complexity: Intermediate
  - Timeframe: Intraday
  - Seasonality: No
  - Neural Networks: No
  - Divergence: No
  - Risk Level: Medium

## Additional Notes

- The Compass smoothing uses a triangular window equal to round(`CompassPeriod` / 3), closely matching the original indicator implementation.
- Session strings such as `0930-1600` restrict trading to the specified window while still updating indicator states outside the session.
- Protective orders reuse StockSharp's high-level helpers so the logic is compatible with portfolio risk management modules.
