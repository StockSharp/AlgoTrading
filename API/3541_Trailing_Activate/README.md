# Trailing Activate Strategy

## Overview
- **Original source**: MetaTrader 5 expert advisor “Trailing Activate.mq5”.
- **Purpose**: manage existing positions by applying the same stepped trailing-stop logic implemented in the MQL5 script.
- **Trading style**: utility/overlay – the strategy never opens new trades, it only adjusts (and, if required, exits) positions that are already running on the attached security.

## Key behaviour
1. The strategy continuously monitors the current long or short position size. When the position becomes flat the internal trailing state is reset automatically.
2. Trailing begins only after the position has accumulated sufficient unrealised profit. The activation threshold mirrors the MQL inputs: the price must advance by `TrailingStopPoints + TrailingStepPoints` **and** the projected stop must be at least `TrailingActivatePoints` away from the entry.
3. Once activated, the stop is shifted in discrete steps. A new trailing price is only accepted if it is improved by at least `TrailingStepPoints` (converted to absolute price by the instrument’s `PriceStep`).
4. When the protective level is penetrated the position is closed with a market order. This emulates `PositionModify` from MetaTrader, which adjusts the broker-side stop-loss. StockSharp does not expose the same helper, therefore the position is exited locally instead.
5. Two execution modes are available:
   - **EveryTick** – driven by level1 best bid/ask updates. This matches the EA’s default `bar_0` mode and keeps the stop in sync with each price change.
   - **NewBar** – updates on completed candles produced by the configured timeframe. This corresponds to `bar_1` in the original script and is useful when the trailing should move only once per bar.

## Parameters
| Name | Description | Default |
| ---- | ----------- | ------- |
| `TrailingMode` | Selects between per-tick and per-bar trailing updates. | `NewBar` |
| `CandleType` | Timeframe for candle subscription when `TrailingMode = NewBar`. | 15 minute time frame |
| `TrailingActivatePoints` | Profit (in points) that must be reached before the trailing logic can engage. | `70` |
| `TrailingStopPoints` | Distance (in points) maintained between price and trailing stop. | `250` |
| `TrailingStepPoints` | Minimum extra profit (in points) before the trailing stop is moved again. | `50` |

All point-based parameters are converted to actual price distances via `Security.PriceStep`. If the price step is missing or zero, a value of `1` is used as a safe fallback, exactly like the MetaTrader engine works with point-to-price conversions.

## Implementation notes
- The strategy subscribes to both candles and level1 data. Only the stream required by the selected `TrailingMode` influences the trailing logic; the other stream remains idle. This keeps the behaviour identical to the EA while still relying on StockSharp’s high-level API.
- Broker-specific limitations such as `SYMBOL_TRADE_STOPS_LEVEL` and `SYMBOL_TRADE_FREEZE_LEVEL` cannot be queried in StockSharp. The conversion therefore checks only the distances configured through the parameters and documents the difference here.
- `Position.AveragePrice` is used as the entry reference. If the strategy is attached to a pre-existing position whose average price is unknown, the current price is used until a new trade supplies precise information.
- Because the stop is kept virtually inside the strategy, the trade is closed via `BuyMarket`/`SellMarket` calls when the trailing level is breached. Users can layer their own physical stop-loss orders on top if exchange-side protection is required.

## Usage
1. Attach the strategy to the security whose position should be managed.
2. Make sure a position is already open (either manually or by another strategy). Trailing begins automatically once profit reaches the activation thresholds.
3. Adjust the point-based parameters to match the instrument’s tick size and the desired trailing characteristics.
4. Choose `EveryTick` for fast-moving instruments or `NewBar` for a more conservative update schedule aligned with candle closes.
