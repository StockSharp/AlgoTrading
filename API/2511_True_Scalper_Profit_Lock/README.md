# True Scalper Profit Lock Strategy

## Overview
The **True Scalper Profit Lock Strategy** is a StockSharp port of the MetaTrader 5 expert advisor "True Scalper Profit Lock". The strategy focuses on ultra short-term trading using fast exponential moving averages, a two-period RSI filter, and a profit protection routine that moves stops to break even. Additional "abandon" logic forces the strategy to close trades that do not reach the target within a predefined number of candles.

The implementation subscribes to a single candle stream and evaluates the finished candles only. It is designed for intraday scalping, but all parameters are fully adjustable, allowing it to be adapted to other timeframes or instruments.

## Indicators and Data
- **EMA (fast)** – default length 3, acts as the bullish trigger when crossing above the slow EMA.
- **EMA (slow)** – default length 7, defines the short-term trend direction.
- **RSI** – default length 2 with selectable decision mode:
  - *Method A* (disabled by default) reacts to RSI crossing the threshold from the previous candle.
  - *Method B* (enabled by default) tracks RSI polarity relative to the threshold.
- **Candles** – default time frame is 1 minute, configurable through the `CandleType` parameter.

## Entry Logic
1. Calculate the fast EMA, slow EMA and RSI on the latest finished candle.
2. Evaluate the RSI state:
   - Method A: set the RSI polarity only when the threshold is crossed between two consecutive candles.
   - Method B: set the RSI polarity according to whether the value is above or below the threshold.
3. **Buy setup** – triggered when the fast EMA is at least one price step above the slow EMA *and* the RSI indicates negative polarity. If the abandon logic forced a reverse to long, the trade is also opened regardless of the current signals.
4. **Sell setup** – triggered when the fast EMA is at least one price step below the slow EMA *and* the RSI indicates positive polarity, or when an abandon reverse enforces a short entry.
5. Position reversals are handled by sending the difference required to flip the net position in a single market order.

## Exit Logic
- **Stop Loss / Take Profit** – configured in price steps (`StopLossPoints`, `TakeProfitPoints`) and applied immediately after entry.
- **Profit Lock** – when enabled, once the open trade accumulates the specified profit (`BreakEvenTriggerPoints`) the stop is moved to break even plus an offset (`BreakEvenPoints`). The routine works for both long and short positions and only runs once per trade.
- **Abandon Logic** – tracks the number of finished candles since entry:
  - *Method A*: closes the trade after `AbandonBars` candles and sets a flag to open a position in the opposite direction on the very next opportunity.
  - *Method B*: closes the position after the timeout but leaves signal-based direction selection untouched.
  - Method A has priority when both methods are enabled.
- Manual exits are issued with market orders (via `ClosePosition`) and automatically reset the trade state.

## Money Management
- When `UseMoneyManagement` is enabled the position size is derived from the portfolio balance: `Ceiling(Balance * RiskPercent / 10000) / 10`.
- The managed volume is bounded to the original MT5 rules: minimum fallback to `InitialVolume`, values above 1 lot rounded up, optional mini-account multiplier, hard cap at 100 lots.
- When disabled the strategy uses the fixed `InitialVolume` for every order.

## Parameters
- `InitialVolume` – base lot size when money management is disabled.
- `TakeProfitPoints` / `StopLossPoints` – distance in `Security.PriceStep` units.
- `FastPeriod`, `SlowPeriod`, `RsiLength`, `RsiThreshold` – indicator configuration.
- `UseRsiMethodA`, `UseRsiMethodB` – toggle the RSI decision logic.
- `UseAbandonMethodA`, `UseAbandonMethodB`, `AbandonBars` – configure the timeout management.
- `UseMoneyManagement`, `RiskPercent`, `LiveTrading`, `IsMiniAccount` – risk sizing options aligned with the MT5 expert advisor.
- `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenPoints` – break-even parameters.
- `MaxPositions` – kept for compatibility with the MQL version (the StockSharp port manages a single net position per instrument).
- `CandleType` – timeframe or custom candle type for signal generation.

## Usage Notes
- Attach the strategy to a single security; the `GetWorkingSecurities` override automatically subscribes to the selected candle type.
- Profit lock and abandon features rely on finished candles; intrabar price spikes that revert within the same candle are ignored.
- The original MT5 parameter `Slippage` was not used in the source code and is therefore not present.
- Adjust `Security.PriceStep` or the step-based parameters according to the traded instrument to maintain the intended pip distances.

## Conversion Differences
- StockSharp operates on net positions, so simultaneous multiple positions are not opened even if `MaxPositions` is greater than one. This mirrors the typical netting behaviour of the original expert when `maxTradesPerPair` equals 1.
- Order management uses `BuyMarket`, `SellMarket`, and `ClosePosition` helpers instead of direct ticket manipulation.
- Indicator data is delivered through `Bind` callbacks to avoid manual buffer access.

## Testing Recommendations
- Validate the behaviour on historical data with the same timeframe used in the original EA (1-minute candles).
- Optimise `TakeProfitPoints`, `StopLossPoints`, and `BreakEvenTriggerPoints` for the target instrument as these were tuned for forex quotes.
