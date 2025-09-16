# Alligator Volatility Strategy

The Alligator Volatility Strategy is a high-level StockSharp port of the "Alligator vol 1.1" MetaTrader expert advisor. It combines Bill Williams' Alligator indicator with optional fractal breakout confirmation, martingale-style averaging orders, and trailing risk management. The module is intended for discretionary traders who want to automate the original workflow while keeping granular control over position sizing and filters.

## Logic overview

- Subscribes to the selected time-frame candles and calculates three smoothed moving averages (jaw, teeth, lips) that form the Alligator indicator.
- Detects bullish phases when the lips stay above the jaw by at least the configured `EntryGap` and remain above the teeth by `ExitGap`. Bearish phases require the jaw to dominate the lips while staying above the teeth.
- Tracks Bill Williams fractals within the latest `FractalBars` candles. The fractal breakout filter is optional and ensures fresh highs for longs or fresh lows for shorts.
- Places an initial market order once a new Alligator state appears. When martingale is enabled, additional averaging limit orders are distributed around multiples of the stop-loss distance with exponential position sizing.
- Manages position exits through take-profit, stop-loss, optional trailing stop, and optional Alligator state reversal.

## Entry rules

1. The strategy waits for finished candles and ignores partial data.
2. A long setup requires one of the following:
   - Alligator entry enabled, the bullish state flips from false to true, and (if enabled) a valid upper fractal is at least `FractalDistancePips` away from the current close.
   - Alligator entry disabled, but (if enabled) the fractal breakout condition still passes.
3. A short setup mirrors the long conditions using the bearish Alligator state and lower fractals.
4. The `ManualMode` parameter blocks automatic entries, allowing discretionary order submission through the UI.
5. When `OnlyOnePosition` is true the strategy refuses to open a new position if an opposite exposure already exists.

## Exit rules

- Initial stops and targets are attached immediately after the position increases. Distances are calculated from the average entry price using `StopLossPips` and `TakeProfitPips` converted with the instrument's price step.
- If `EnableTrailing` is true, the stop follows price after the trade gains at least `TrailingActivationPips` of profit. Longs trail below the highest candle close/high, shorts trail above the lowest close/low.
- When `UseAlligatorExit` is active, the position closes once the Alligator state collapses (bullish state disappears for longs or bearish state disappears for shorts).
- Hitting the take-profit or stop-loss price closes the position and cancels pending averaging orders on that side.

## Martingale grid

- `EnableMartingale` activates a ladder of limit orders after the market entry.
- Each step multiplies the previous executed volume by `2 * MartingaleMultiplier` (capped at `MaxVolume`).
- Limit prices are spaced by the stop-loss distance (`StopLossPips`) and shifted by `GridSpreadPips` to compensate for the broker spread.
- Pending orders are cancelled whenever a new signal is processed, the position is flattened, or a manual exit occurs.

## Money management

- Order volume is calculated from the account equity using `RiskPerThousand`: `volume = equity / 1000 * RiskPerThousand`.
- `MinVolume` acts as fallback when the equity information is not available. `MaxVolume` caps both the initial trade and martingale steps.
- All prices are rounded to the nearest exchange tick before submitting orders.

## Parameters

| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Data type used for candle subscription. | 15-minute time frame |
| `ManualMode` | Disable automatic entries when true. | `false` |
| `UseAlligatorEntry` | Require Alligator expansion before entering. | `true` |
| `UseFractalFilter` | Enforce fractal breakout confirmation. | `false` |
| `UseAlligatorExit` | Close trades when the Alligator collapses. | `false` |
| `OnlyOnePosition` | Allow only a single open position. | `true` |
| `EnableMartingale` | Add averaging limit orders. | `true` |
| `EnableTrailing` | Activate trailing stop management. | `true` |
| `RiskPerThousand` | Equity-based volume multiplier. | `0.04` |
| `MaxVolume` | Maximum allowed order size. | `0.5` |
| `MinVolume` | Fallback order size. | `0.01` |
| `StopLossPips` / `TakeProfitPips` | Distance to stop and target in pips. | `80` |
| `TrailingStopPips` | Trailing stop distance in pips. | `30` |
| `TrailingActivationPips` | Profit required before trailing adjusts. | `20` |
| `EntryGap` | Minimum gap between lips and jaw (price units). | `0.0005` |
| `ExitGap` | Minimum separation from the teeth (price units). | `0.0001` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | SMMA lengths for the Alligator lines. | `13 / 8 / 5` |
| `JawShift`, `TeethShift`, `LipsShift` | Bar shift applied when evaluating signals. | `8 / 5 / 3` |
| `FractalBars` | Number of candles scanned for fractals. | `10` |
| `FractalDistancePips` | Required distance between price and fractal. | `30` |
| `MartingaleDepth` | Number of averaging limit orders. | `10` |
| `MartingaleMultiplier` | Additional multiplier for averaging volume. | `1.3` |
| `GridSpreadPips` | Spread offset applied to the grid. | `10` |

## Notes

- The Alligator indicator is processed on candle medians and uses one-bar delays to avoid working with unfinished values.
- `EntryGap` and `ExitGap` are expressed in absolute price units. Adjust them to match the instrument's tick size if necessary.
- Fractal detection mirrors the standard five-bar Bill Williams pattern. When the filter is active it ignores setups until enough history is collected.
- The strategy does not create protective stop or take-profit orders on the exchange. All exits are handled internally by the strategy logic.
- Manual changes to pending or active orders are supported; the strategy cleans its internal grids when orders are filled or cancelled.
