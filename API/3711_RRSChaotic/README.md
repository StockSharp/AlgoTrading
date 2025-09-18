# RRS Chaotic Strategy

## Overview
The original **RRS Chaotic EA** continuously rolls the dice on every tick, choosing random symbols and position sizes before dispatching market orders. The StockSharp port keeps the spirit of controlled chaos by driving entries from a candle stream on the configured security. Each closed candle triggers a new random decision for both direction and volume while mirroring the expert advisor's money-management rules.

## Key Features
- **Random entries** – every finished candle generates a random integer from 0 to 10. Values `6` or `9` open a long position, while `3` or `8` open a short position, matching the MT4 logic.
- **Variable volume** – the traded volume is sampled uniformly between the *MinVolume* and *MaxVolume* parameters and aligned with the security's volume step.
- **Spread filter** – new positions are blocked whenever the current spread (in points) exceeds *MaxSpreadPoints*.
- **Take-profit & stop-loss** – optional point-based exits that recreate the order-level settings of the expert.
- **Drawdown guard** – unrealized loss is continuously compared against either a fixed cash limit or a percentage of portfolio value. Breaching the limit cancels active orders and flattens the position.

## Parameters
| Name | Description |
|------|-------------|
| `CandleType` | Candle series used to trigger the strategy (default 1-minute candles). |
| `MinVolume` / `MaxVolume` | Range for random lot generation. |
| `TakeProfitPoints` | Take-profit distance in price points. Set to `0` to disable. |
| `StopLossPoints` | Stop-loss distance in price points. Set to `0` to disable. |
| `MaxOpenTrades` | Maximum net volume measured in volume steps that may remain open simultaneously. |
| `MaxSpreadPoints` | Maximum allowed spread, expressed in price points. |
| `SlippagePoints` | Informational slippage parameter (kept for completeness). |
| `RiskControlMode` | Selects between `FixedMoney` and `BalancePercentage` risk models. |
| `RiskValue` | Either the amount of money to risk or the percentage of equity, depending on the mode. |
| `TradeComment` | Tag appended to generated orders for easier audit. |

## Strategy Logic
1. Subscribe to the configured candle series and wait for finished candles.
2. Apply drawdown control. If unrealized loss breaches the threshold, cancel active orders and close the current position.
3. Maintain optional stop-loss and take-profit targets that mirror the MT4 order settings.
4. When trading is allowed and the spread is acceptable, roll a random number to decide whether to open a long or short position.
5. Cap the accumulated exposure by limiting the number of volume steps to `MaxOpenTrades`.

## Differences vs. MQL4 Version
- The original expert traded across multiple random symbols. StockSharp strategies operate on a single security; therefore, randomness is applied to direction and size only.
- Protective stops are executed via market orders on candle closes instead of native stop-loss/take-profit order parameters.
- Spread evaluation uses the current best bid/ask instead of the MT4 `MarketInfo` function.
- All generated orders include the *TradeComment* text, providing context similar to the MT4 magic numbers.

## Usage Notes
- Ensure the connected security exposes valid `PriceStep`, `MinStep`, and `VolumeStep` values for accurate point-to-price conversion.
- The default candle timeframe is one minute to emulate tick-level randomness without overwhelming the backtesting pipeline. Increase the timeframe to reduce trading frequency.
- Risk control relies on unrealized PnL derived from the aggregated position. Mixed long/short baskets, as seen in the MT4 version, are not supported by StockSharp and are therefore netted.
