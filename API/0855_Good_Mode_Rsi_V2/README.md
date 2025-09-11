# Good Mode RSI v2 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades RSI extremes with custom take-profit and trailing stop thresholds. It sells when RSI surpasses a high level and closes when RSI falls to a profit-taking value. It buys when RSI dips to a low level and closes when RSI rises to a profit target. In both cases, a trailing stop follows the most favorable price to protect gains.

## Details

- **Entry Criteria**:
  - **Long**: `RSI < buy level`.
  - **Short**: `RSI > sell level`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - **Long**: `RSI > take profit level buy` or trailing stop hit.
  - **Short**: `RSI < take profit level sell` or trailing stop hit.
- **Stops**: Trailing stop in ticks.
- **Default Values**:
  - `RSI Period` = 2
  - `Sell Level` = 96
  - `Buy Level` = 4
  - `Take Profit Level Sell` = 20
  - `Take Profit Level Buy` = 80
  - `Trailing Stop Offset` = 100
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: Single
  - Stops: Yes
  - Complexity: Basic
  - Timeframe: Intraday
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium
