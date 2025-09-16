# Zone Recovery Area Strategy

## Overview
The **Zone Recovery Area Strategy** is a direct conversion of the MetaTrader expert advisor "Zone Recovery Area" (package `MQL/20266`). It recreates the original hedging logic on top of the StockSharp high-level API and adds exhaustive parameterization so the behaviour can be tuned without touching the code. The strategy combines a trend filter with an alternating buy/sell recovery grid: once a primary trade is opened, additional positions are stacked whenever price leaves or re-enters the predefined zone, creating a hedged basket that aims to recover floating drawdowns.

Core characteristics:
- Uses a fast/slow simple moving average crossover together with a monthly MACD filter to define the trading bias.
- Implements the zone recovery technique: the first trade establishes a base price, and alternating hedge orders are fired whenever the market crosses the zone boundary or returns to the base level.
- Provides money-based, percentage-based, and trailing profit controls to exit the basket once sufficient profit has been locked in.
- Allows both multiplicative (martingale-style) and additive position sizing for each recovery step.

## Market Data & Indicators
- **Primary candles:** user-defined timeframe (default 30 minutes) for entries and recovery management.
- **Monthly candles:** constructed from lower timeframes if needed; used to compute MACD (12/26/9) values.
- **Indicators:**
  - Simple Moving Average (fast and slow) on the primary timeframe.
  - Moving Average Convergence Divergence with signal line on the monthly timeframe.

## Trading Logic
1. **Trend Validation**
   - Wait until both SMAs and the monthly MACD are fully formed.
   - A bullish setup requires the fast SMA to be below the slow SMA on the previous bar while the monthly MACD line is above its signal.
   - A bearish setup requires the fast SMA to be above the slow SMA on the previous bar while the monthly MACD line is below its signal.
2. **Cycle Initialisation**
   - When a bullish (bearish) setup is detected, open the initial long (short) position with `InitialVolume` and store the entry price as the cycle base.
   - Reset internal counters and profit tracking for the new cycle.
3. **Zone Recovery Engine**
   - Define two critical levels: the **zone boundary** (`ZoneRecoveryPips`) away from the base price and the **take-profit level** (`TakeProfitPips`) in the favourable direction.
   - While the cycle is active, monitor each completed candle:
     - If price reaches the take-profit level, close all net exposure and finish the cycle.
     - If money or percent profit targets are met, or the trailing profit lock is triggered, close the cycle.
     - Otherwise, evaluate if a new hedge is needed:
       - For long cycles: open an additional short when price drops below `base - zone`, and open an additional long when price trades back above the base price.
       - For short cycles: open an additional long when price rises above `base + zone`, and open an additional short when price returns below the base price.
     - Hedge direction alternates automatically; the next order size is determined either by multiplying the previous volume or by adding a fixed increment.
   - The number of trades per basket is capped by `MaxTrades`.
4. **Profit Management**
   - `UseMoneyTakeProfit`: close the basket once unrealised profit reaches the configured currency amount.
   - `UsePercentTakeProfit`: close the basket once unrealised profit equals the specified percentage of the portfolio value.
   - `EnableTrailing`: once profit exceeds `TrailingStartProfit`, track the peak and exit the cycle if profit falls by `TrailingDrawdown`.

All orders are placed using StockSharp high-level helpers (`BuyMarket`/`SellMarket`), which keeps the implementation consistent with the framework best practices.

## Parameters
| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | 30-minute candles | Timeframe for entries and recovery monitoring. |
| `MonthlyCandleType` | 30-day candles | Higher timeframe used to build the MACD trend filter. |
| `FastMaLength` | 20 | Period of the fast SMA. |
| `SlowMaLength` | 200 | Period of the slow SMA. |
| `TakeProfitPips` | 150 | Distance from the base price to close the entire basket in profit. |
| `ZoneRecoveryPips` | 50 | Half-width of the hedging zone around the base price. |
| `InitialVolume` | 1 | Volume of the first trade in each cycle. |
| `UseVolumeMultiplier` | true | If enabled, each new hedge multiplies the previous volume. |
| `VolumeMultiplier` | 2 | Factor applied to the previous volume when `UseVolumeMultiplier` is true. |
| `VolumeIncrement` | 0.5 | Additive volume increase when `UseVolumeMultiplier` is false. |
| `MaxTrades` | 6 | Maximum number of trades per recovery cycle (including the initial one). |
| `UseMoneyTakeProfit` | false | Enable money-based take profit. |
| `MoneyTakeProfit` | 40 | Profit target in account currency. |
| `UsePercentTakeProfit` | false | Enable percentage-based take profit. |
| `PercentTakeProfit` | 5 | Profit target as a percentage of portfolio value. |
| `EnableTrailing` | true | Enable trailing profit protection. |
| `TrailingStartProfit` | 40 | Profit threshold required before trailing becomes active. |
| `TrailingDrawdown` | 10 | Allowed profit giveback once trailing is active. |

> **Pip Conversion:** `TakeProfitPips` and `ZoneRecoveryPips` are converted into price offsets using the security price step. Ensure the traded instrument provides correct `PriceStep` and `StepPrice` values.

## Usage Notes
1. Add the strategy to your StockSharp solution (Designer, API, Runner, etc.).
2. Assign the desired security and portfolio before starting.
3. Adjust the parameters to match instrument volatility, acceptable drawdown, and account size.
4. Ensure sufficient historical data so that both SMAs and the monthly MACD can warm up before the first trade.
5. Monitor margin usage carefully: recovery steps can quickly increase exposure, especially when the multiplier is enabled.

## Risk Management & Considerations
- Zone recovery/martingale techniques can accumulate very large positions in trending markets. Always test with conservative settings and use the `MaxTrades` parameter to bound risk.
- Because StockSharp maintains a single net position, the internal profit calculation replicates the basket PnL using security price/step information. Validate the figures with your broker data feed.
- Money and percentage targets rely on portfolio valuation. When backtesting or paper trading, make sure the portfolio model supplies `BeginValue`/`CurrentValue` correctly.
- No automatic hard stop-loss is used; risk is managed via the recovery mechanics. Consider combining the strategy with external portfolio-level stops.

## Files
- `CS/ZoneRecoveryAreaStrategy.cs` — implementation of the strategy.
- `README.md` — English documentation (this file).
- `README_ru.md` — Russian documentation.
- `README_cn.md` — Chinese documentation.

