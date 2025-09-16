# ColorMETRO WPR Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy uses the ColorMETRO Williams %R indicator, which builds fast and slow step lines around the Williams %R oscillator.
The fast line reacts quickly to price changes, while the slow line smooths out noise. Trading decisions are made when these lines
cross each other, signalling potential shifts in momentum. When the fast line crosses below the slow line, the strategy assumes the
market is oversold and enters a long position. Conversely, when the fast line crosses above the slow line, it enters a short position.
Existing positions are exited when the opposite condition is detected.

Risk management is handled through percentage-based take profit and stop loss levels. These allow the strategy to adapt to price
levels across different instruments. The default candle timeframe is eight hours, which helps to filter out intraday volatility and
focus on medium-term trends. The logic works on both sides of the market, enabling long and short operations.

## Details

- **Entry Criteria**:
  - **Long**: `Fast line` crosses **below** `Slow line`.
  - **Short**: `Fast line` crosses **above** `Slow line`.
- **Long/Short**: Both sides.
- **Exit Criteria**:
  - **Long**: `Fast line` rises above `Slow line`.
  - **Short**: `Fast line` falls below `Slow line`.
- **Stops**: Yes, percentage-based take profit and stop loss.
- **Default Values**:
  - `WprPeriod` = 7.
  - `FastStep` = 5.
  - `SlowStep` = 15.
  - `TakeProfitPercent` = 4.
  - `StopLossPercent` = 2.
  - `CandleType` = 8-hour candles.
- **Filters**:
  - Category: Trend following.
  - Direction: Both.
  - Indicators: Single (Williams %R based).
  - Stops: Yes.
  - Complexity: Medium.
  - Timeframe: Medium-term.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
