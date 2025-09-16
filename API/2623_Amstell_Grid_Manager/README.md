# Amstell Grid Manager Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

High-level port of the MetaTrader expert "exp_Amstell-SL" that runs a bi-directional averaging grid. The strategy keeps track of the most recent fill price on each side and issues additional market orders when price drifts far enough away, while liquidating the open batch once a fixed take-profit or stop-loss distance is reached. The implementation uses StockSharp's candle subscriptions and high-level order helpers, so it can be plugged into any environment that provides candle data for a single security.

The translated logic is slightly adapted for StockSharp's netted portfolio model: long and short grids are still managed separately, but they are not held at the same time. The long grid is active while the net position is non-negative, and the short grid takes over only after all long exposure has been flattened.

## How it works

### Market data and execution flow
- Subscribes to the configured `CandleType` (default: 1 minute time-frame candles) and processes only finished candles.
- Calculates pip-based offsets from the security's `PriceStep`. If the step has 3 or 5 decimal places, it is multiplied by 10 to mimic MetaTrader's 3/5 digit pip adjustment.
- All trades are placed through `BuyMarket`/`SellMarket` helpers; no pending orders are used.

### Long-side management
- Opens the first long position (`OrderVolume`) as soon as there is no existing long exposure and the strategy is not in the middle of closing shorts.
- Tracks the most recent long fill price and the volume-weighted average entry price for the active long batch.
- Places additional long orders of size `OrderVolume` whenever the closing price has fallen by at least `BuyDistancePips` (converted to price units) below the last long fill.

### Short-side management
- Once the long batch is fully closed and the net position is non-positive, the strategy allows short entries.
- Places the initial short order when there is no short exposure; further shorts are opened after the price rises by `BuyDistancePips * SellDistanceMultiplier` above the previous short fill.
- Maintains the most recent short fill price and the volume-weighted average entry price for the active short batch.

### Exit rules
- For each direction, computes unrealised profit relative to the average fill.
- Closes the entire long batch with a market sell when profit reaches `TakeProfitPips` pips or the drawdown reaches `StopLossPips` pips.
- Closes the entire short batch with a market buy when profit reaches `TakeProfitPips` pips or the adverse move reaches `StopLossPips` pips.
- After liquidation, all cached prices and volumes are reset so a new grid can start on the next candle.

### Differences versus the original MQL expert
- The StockSharp version operates on candle closes instead of individual ticks.
- Long and short grids are executed sequentially rather than simultaneously, matching StockSharp's default netting mode.
- All protective distances are checked against the averaged entry price instead of each ticket individually, which mirrors the aggregate net position behaviour.

## Parameters

| Parameter | Default | Optimization range | Description |
|-----------|---------|--------------------|-------------|
| `OrderVolume` | `0.01` | `0.01` – `0.10` (step `0.01`) | Quantity submitted with every grid order. Must be positive. |
| `TakeProfitPips` | `30` | `10` – `150` (step `10`) | Profit target for the active batch expressed in pips. |
| `StopLossPips` | `30` | `10` – `150` (step `10`) | Maximum adverse move before abandoning the batch. |
| `BuyDistancePips` | `10` | `5` – `60` (step `5`) | Minimum drop from the last long fill to add another buy. Must be less than both TP and SL. |
| `SellDistanceMultiplier` | `10` | `2` – `15` (step `1`) | Multiplier applied to the long distance when spacing short entries. |
| `CandleType` | 1-minute time-frame | — | Candle series used for signal generation. |

## Implementation notes
- `BuyDistancePips` must be strictly less than `TakeProfitPips` and `StopLossPips`; the strategy throws an exception at start-up otherwise, reproducing the MetaTrader validation.
- Pip size is derived from the security's `PriceStep`. Adjust the parameters if the instrument uses a non-standard tick size.
- All internal state is cleared in `OnReseted`, allowing the strategy to be restarted without residual grid data.
- No colour customisation or manual indicator registration is used, matching the high-level API guidelines in this repository.
