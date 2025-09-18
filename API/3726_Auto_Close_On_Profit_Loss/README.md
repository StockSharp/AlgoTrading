# Auto Close On Profit/Loss Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This utility strategy continuously monitors the floating profit and loss of the current portfolio and automatically flattens every open position once a configured profit target or loss threshold is reached. It is a StockSharp port of the MetaTrader expert advisor "AutoCloseOnProfitLoss".

The strategy does not open positions on its own. Instead, it acts as a risk/profit management layer that watches existing trades and sends market orders to close them when the configured monetary objectives are met.

## Details

- **Entry Criteria**: None. The strategy never opens trades; it only supervises existing exposure.
- **Long/Short**: Both. Any long or short positions in the portfolio can be closed.
- **Exit Criteria**:
  - All positions are closed when the floating profit is greater than or equal to `TargetProfit` and `EnableProfitClose` is enabled.
  - All positions are closed when the floating loss is less than or equal to `MaxLoss` (a negative value) and `EnableLossClose` is enabled.
  - The floating profit and loss is computed in portfolio currency using the aggregated portfolio profit if available, otherwise by summing the PnL of each open position.
- **Stops**: Implicit monetary stop using the `MaxLoss` parameter.
- **Default Values**:
  - `TargetProfit` = 100 (portfolio currency units).
  - `MaxLoss` = -50 (portfolio currency units).
  - `EnableProfitClose` = true.
  - `EnableLossClose` = true.
  - `ShowAlerts` = true (writes detailed information to the strategy log when closing positions).
  - `CandleType` = 1-minute timeframe (used only as a timer source for periodic checks).
- **Filters / Tags**:
  - Category: Utility / Risk management.
  - Direction: Both.
  - Indicators: None.
  - Stops: Monetary.
  - Complexity: Low.
  - Timeframe: Configurable timer candles (default 1 minute).
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Determined by configured thresholds.

## Implementation Notes

- The strategy subscribes to a configurable candle series solely to obtain periodic callbacks, mimicking the tick-by-tick checks performed in the original MetaTrader expert.
- Whenever the exit condition becomes true it repeatedly sends market orders to flatten every security found in the strategy and portfolio until no open positions remain.
- Parameter validation ensures `TargetProfit` is positive when the profit rule is enabled and `MaxLoss` is negative when the loss rule is enabled, preventing misconfiguration.
