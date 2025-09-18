# TRAYLERv Strategy

## Overview
The **TRAYLERv Strategy** is a direct conversion of the MetaTrader 4 expert advisor *TRAYLERv*. The original code acted as an automated trade manager rather than a signal generator; it continuously adjusted protective orders for existing positions using Bill Williams fractals and allowed traders to clean up outstanding pending orders. This StockSharp port preserves the same behaviour while leveraging the high-level API for order management and candle subscriptions.

The strategy does **not** open positions on its own. It expects trades to be created manually or by another strategy and then takes over the job of maintaining stops and take-profits according to the logic below. All comments and configuration names follow the legacy EA so that experienced users can map the behaviour quickly.

## Trading Logic
1. Subscribe to the configured candle series (one-minute candles by default) and record each finished bar. Fractal highs and lows are detected once five candles are available, reproducing the standard MT4 fractal definition.
2. Every time a new candle closes during an even minute, the strategy checks the current net position:
   - **Long positions**: search for the most recent down fractal within `StopFractalDepth` bars (default 7). If found, place or move a sell stop below the fractal low minus the current spread and a two-point buffer. If no valid fractal exists, use the low of the candle three bars back minus two points. When a long position is profitable and take-profits are enabled, look for the latest up fractal within `TakeProfitFractalDepth` bars (default 21) and place a sell limit slightly below that level to match the MetaTrader implementation.
   - **Short positions**: mirror the logic using up fractals for the trailing buy stop and down fractals for the take-profit target. Buffers are added above the fractal highs to avoid premature stops.
3. When `DeleteAllPendingOrders` is enabled the strategy cancels every active pending order it can see. Alternatively, `DeleteOwnPendingOrders` removes only the pending orders that belong to the current symbol. Both options replicate the manual clean-up switches from the original EA.
4. If no position is open, all protective orders registered by the strategy are cancelled to keep the order book tidy.

## Risk Management
- Protective orders are created with market order counterparts (`SellStop`, `BuyStop`, `SellLimit`, `BuyLimit`). The volume of the protective order always matches the absolute net position size.
- Trailing stops and take-profits are optional. Disabling the take-profit parameter removes any existing limit order but leaves the trailing logic intact.
- Spread information is taken from the best bid/ask pair when available. If no spread can be measured, the code falls back to the minimal price increment of the instrument to avoid placing orders directly on the current price.
- All price levels are normalised to the instrument’s tick size so that the resulting orders comply with exchange requirements.

## Parameters
| Parameter | Description | Default |
|-----------|-------------|---------|
| `OrderVolume` | Suggested default volume for manual entries. It is kept for compatibility with the original EA and is not used internally. | `0.1` |
| `DeleteAllPendingOrders` | When `true`, cancel every active pending order on the connection after each candle. | `false` |
| `DeleteOwnPendingOrders` | When `true`, cancel only the pending orders for the current symbol. | `false` |
| `UseTakeProfit` | Enables fractal-based take-profit calculation. When disabled, any existing take-profit order is removed. | `true` |
| `EnableSound` | Preserved legacy flag from MT4; provided for completeness but not used in StockSharp. | `true` |
| `ShowCommentary` | Legacy switch equivalent to the MT4 on-chart commentary. It is available for configuration screens but has no effect in the port. | `true` |
| `StopFractalDepth` | Number of bars inspected to find a fractal for the trailing stop. | `7` |
| `TakeProfitFractalDepth` | Number of bars inspected to find a fractal for the take-profit. | `21` |
| `CandleType` | Data type used for the primary candle series. Defaults to a 1-minute time frame. | `1 minute` time frame |

## Implementation Notes
- The strategy uses the high-level `SubscribeCandles().Bind(...)` workflow and processes only finished candles, mirroring the MT4 tick-based loop while avoiding premature updates.
- Fractal detection is implemented manually using a rolling list of candle snapshots. This reproduces the behaviour of the MT4 `iFractals` indicator without relying on extra StockSharp indicators.
- Order prices are rounded to the nearest valid tick, and volumes respect `VolumeStep`, `MinVolume`, and `MaxVolume` constraints to guarantee exchange compatibility.
- No Python translation is included. The `PY` directory is intentionally absent, matching the requirements of the conversion guidelines.
