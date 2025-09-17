# At Random Full Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

## Overview
The **At Random Full Strategy** is a faithful conversion of the MetaTrader 5 expert advisor "At random Full". It keeps the 
original idea of opening trades based on a random generator while exposing the same money-management switches: direction 
filters, grid spacing, optional time windows and an on/off toggle for averaging. The StockSharp port uses the high-level API, 
so the entire decision loop is driven by candle subscriptions and standard `StartProtection` helpers for protective orders.

## Trading Logic
1. On every finished candle the strategy verifies that trading is allowed (session filter, portfolio state and optional 
   "only one position" flag).
2. A pseudo-random generator decides between a long or short entry. The `ReverseSignals` parameter can flip the outcome to 
   emulate the MQL reverse mode.
3. Direction filters (`TradeMode`) block undesired signals. The code also enforces the original EA rule of a single trade per 
   bar in each direction by remembering the candle open time of the latest signal.
4. Grid management options mirror the MetaTrader behaviour:
   - `MaxPositions` caps the number of averaged entries per side.
   - `MinStepPoints` requires a minimum distance (converted to price using the security price step) between consecutive entries.
   - `CloseOpposite` forces the existing opposite exposure to be closed before a new trade is sent.
5. Market orders are issued through `BuyMarket` / `SellMarket` with a normalised volume defined by `OrderVolume`.

## Position and Risk Management
- `StartProtection` attaches stop-loss and take-profit orders that match the MetaTrader inputs. If `TrailingStopPoints` is 
  greater than zero the built-in StockSharp trailing mode is enabled. The parameters `TrailingActivatePoints` and 
  `TrailingStepPoints` are converted to price distances and logged for transparency, but the actual trailing is handled by the 
  platform.
- All volume calculations respect the exchange metadata (minimum, maximum and step) exactly like the MQL helper routines.
- Time control emulates the `InpTimeControl` block from the script. When enabled, trades are allowed only inside the configured 
  `[SessionStart, SessionEnd]` window; overnight sessions are supported.

## Parameters
| Parameter | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series used to schedule the decision loop. | `15 minute timeframe` |
| `OrderVolume` | Base market order volume in lots. | `0.1` |
| `MaxPositions` | Maximum number of averaged entries per direction (0 = unlimited). | `5` |
| `MinStepPoints` | Minimum distance between entries expressed in MetaTrader points. | `150` |
| `StopLossPoints` | Stop-loss distance in points. | `150` |
| `TakeProfitPoints` | Take-profit distance in points. | `460` |
| `TrailingActivatePoints` | Profit threshold (in points) logged for informational purposes when trailing is enabled. | `70` |
| `TrailingStopPoints` | Trailing stop distance passed to `StartProtection`. | `250` |
| `TrailingStepPoints` | Step between trailing adjustments, logged alongside the activation distance. | `50` |
| `OnlyOnePosition` | Blocks new trades until the current net position is closed. | `false` |
| `CloseOpposite` | Closes the opposite exposure before opening a trade. | `false` |
| `ReverseSignals` | Inverts the random decision so buys become sells and vice versa. | `false` |
| `UseTimeControl` | Enables the trading session time filter. | `false` |
| `SessionStart` | Session start time (inclusive) when `UseTimeControl` is `true`. | `10:01` |
| `SessionEnd` | Session end time (inclusive) when `UseTimeControl` is `true`. | `15:02` |
| `Mode` | Allowed trade direction (`Both`, `BuyOnly`, `SellOnly`). | `Both` |
| `RandomSeed` | Optional deterministic seed for the pseudo-random generator (0 = environment tick count). | `0` |

## Implementation Notes
- All comments are written in English and the code uses tab indentation, matching the repository guidelines.
- Candle processing relies on `SubscribeCandles().Bind(...)`, ensuring the logic executes once per finished bar as in the EA.
- The strategy keeps track of the last buy and sell fill prices to enforce the minimum spacing constraint even during averaging.
- Logging statements mirror the detailed diagnostics printed by the original script: every entry announces the chosen direction, 
  entry price, volume, and the trailing configuration on startup.

## Usage Tips
- Because the trading signal is random, the strategy is best suited for testing infrastructure or demonstrating risk controls.
- Adjust `OrderVolume`, `StopLossPoints`, and `TakeProfitPoints` to align with the tick size and volatility of the instrument you 
  plan to trade.
- Enable `UseTimeControl` if the EA should operate only during a specific session (for example, the London or New York session).
- Use `RandomSeed` during optimisation runs to achieve reproducible sequences of random decisions.
