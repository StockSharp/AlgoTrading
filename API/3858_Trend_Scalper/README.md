# Trend Scalper Strategy (API/3858)

## Overview
The **TrendScalperStrategy** is a C# conversion of the MetaTrader 4 expert advisor `Currencyprofits_01_1.mq4`. The original robot is a lightweight trend-following scalper that combines a short-term EMA/SMA crossover filter with breakout entries around the most recent swing highs and lows. The StockSharp port keeps the same decision rules while embracing the framework's high-level candle subscription and indicator pipeline.

## Trading Logic
1. **Indicators**
   - Fast EMA (default 6) on close prices.
   - Slow SMA (default 12) on close prices.
   - Highest High (default window 6) and Lowest Low (default window 6) calculated from the candle highs and lows.
2. **Entry Conditions**
   - **Long**: price sweeps into the recent low band (`Lowest Low`) while the fast EMA is above the slow SMA. The strategy sends a market buy order with the volume defined by the money-management rule.
   - **Short**: price touches the recent high band (`Highest High`) while the fast EMA is below the slow SMA. A market sell order is placed using the same volume calculation.
   - The system stays flat while a position is open, mirroring the single-order behaviour of the MQL version.
3. **Exit Conditions**
   - **Long exit**: when an open long position sees the candle high break the recorded `Highest High`, the position is closed at market.
   - **Short exit**: when an open short position observes the candle low fall through the `Lowest Low`, the short is covered at market.
   - A protective stop-loss managed by `StartProtection` is attached to every trade when `StopLossPoints` is greater than zero.

## Money Management
The lot sizing logic reproduces the three modes exposed in the MQL script:

| Mode | Description | Behaviour in the port |
|------|-------------|-----------------------|
| `0`  | Fixed lots (`LotsIfNoMM`). | Returns the configured `FixedVolume`. |
| `<0` | Fractional lots computed from the account balance and risk factor. | Calculates `ceil(balance * risk / 10000) / 10`, capped at 100 lots. |
| `>0` | Whole-lot scaling from balance and risk factor. | Uses the same base formula, but the result is rounded up to the next integer, floored at 1 lot and capped at 100. |

The balance is taken from `Portfolio.CurrentValue` (falling back to `BeginValue`). If the portfolio value is unavailable, the strategy reverts to the fixed volume so orders are still emitted during backtests.

## Risk Management
- **Stop-loss**: the `StopLossPoints` parameter is expressed in price points (pips). During `OnStarted` the distance is multiplied by `Security.PriceStep` and passed to `StartProtection`, letting StockSharp maintain the protective order.
- **Single position**: the logic enforces `Position == 0` before opening a new trade, preventing overlapping positions exactly like the MT4 expert.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `CandleType` | 15-minute time frame | Candle series used for indicator calculations and signals. |
| `FastLength` | 6 | Period of the fast EMA. |
| `SlowLength` | 12 | Period of the slow SMA. |
| `BreakoutWindow` | 6 | Number of candles inspected for the highest high / lowest low breakout filter. |
| `FixedVolume` | 0.1 lots | Volume when money management is disabled or fallback is required. |
| `MoneyManagementMode` | 0 | Selects between fixed, fractional, or rounded lot sizing. |
| `MoneyManagementRisk` | 40 | Risk factor multiplier used in balance-based lot sizing. |
| `StopLossPoints` | 50 | Stop-loss distance in price points (converted to absolute price before calling `StartProtection`). |

## Implementation Notes
- Indicator chaining relies on the high-level `SubscribeCandles().Bind(...)` workflow; no manual series buffering is required.
- Comments in the code were added in English to match the repository guidelines.
- No unit tests were modified; the focus of this conversion is the strategy and its accompanying documentation.

## Usage Tips
- Select a candle interval that matches the original trading environment (e.g., short intraday time frames for scalping).
- Ensure the portfolio has a valid `PriceStep` so the stop-loss conversion to absolute price works correctly.
- Adjust `MoneyManagementRisk` carefully: higher values lead to larger positions because of the `ceil(balance * risk / 10000)` calculation inherited from the MQL expert.
