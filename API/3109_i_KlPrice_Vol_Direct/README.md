# Exp i-KlPrice Vol Direct Strategy

## Overview
The **Exp i-KlPrice Vol Direct Strategy** is a StockSharp adaptation of the MetaTrader 5 expert advisor `Exp_i-KlPrice_Vol_Direct`. The original system multiplies a custom KlPrice oscillator by volume, smooths it with several moving-average stages, and reacts to changes in the slope of the resulting line. The port keeps the multi-stage processing chain, exposes the same configurable parameters, and executes trades through StockSharp’s high-level API on completed candles.

Key ideas preserved from the MQL5 version:
- **Two-stage smoothing of price and range** – price data is filtered by a configurable moving average, the high-low range is smoothed separately, and the results form adaptive dynamic bands.
- **Volume weighting** – the oscillator output is multiplied by the selected volume stream (tick or real) before a final Jurik filter, amplifying moves that occur on higher activity.
- **Directional colour map** – the strategy monitors the sign of the smoothed oscillator slope. A flip from bearish to bullish colour opens longs and closes shorts; the opposite flip opens shorts and closes longs.
- **Signal delay** – `SignalBar` lets the user require additional closed candles before acting, matching the “confirmed bar” logic of the source code.

## Processing Pipeline
1. **Applied Price Selection** – choose from the same twelve applied-price formulas as the MQL indicator (Close, Open, Median, Demark, TrendFollow, etc.).
2. **Primary Smoothing** – apply `PriceMethod` over `PriceLength` bars with optional `PricePhase` (effective for Jurik-based filters). The supported algorithms map the original SmoothAlgorithms library to StockSharp indicators:
   - `Sma` → `SimpleMovingAverage`
   - `Ema` → `ExponentialMovingAverage`
   - `Smma` → `SmoothedMovingAverage`
   - `Lwma` → `WeightedMovingAverage`
   - `Jjma` → `JurikMovingAverage` (phase honoured when available)
   - `Jurx` → `ZeroLagExponentialMovingAverage`
   - `Parma` → `ArnaudLegouxMovingAverage` (phase mapped to ALMA offset)
   - `T3` → `TripleExponentialMovingAverage`
   - `Vidya` → approximated with `ExponentialMovingAverage`
   - `Ama` → `KaufmanAdaptiveMovingAverage`
3. **Range Smoothing** – repeat the same procedure for the candle range (`High - Low`) using `RangeMethod`, `RangeLength`, and `RangePhase`. The smoothed range builds adaptive bands around the price smoother.
4. **Oscillator Construction** – compute `(Price - (PriceMA - RangeMA)) / (2 * RangeMA) * 100 - 50`, identical to the MQL formula, and multiply by the selected volume stream (`VolumeSource`).
5. **Final Jurik Filter** – the weighted oscillator and the raw volume stream are both passed through Jurik moving averages with period `ResultLength` (phase fixed to 100, mirroring the EA).
6. **Colour Detection** – compare the latest smoothed oscillator value with the previous one. Rising values colour the bar bullish (`0`), falling values colour it bearish (`1`), equal values inherit the prior colour. Colours are stored chronologically to honour the `SignalBar` delay.

## Trading Logic
### Long Side
- **Entry**: When the colour at the signal bar (`SignalBar`) is bullish (`0`) and the immediately older colour is bearish (`1`), open a long position if `AllowLongEntries = true` and the current net position is non-positive. The order size equals `Volume + |Position|` to flatten opposing exposure first.
- **Exit**: If the signal-bar colour is bullish and `AllowShortExits = true`, close any open short positions.

### Short Side
- **Entry**: When the signal-bar colour turns bearish (`1`) after being bullish (`0`), open a short position if `AllowShortEntries = true` and the current net position is non-negative.
- **Exit**: If the signal-bar colour is bearish and `AllowLongExits = true`, close existing long exposure.

Trades are generated on finished candles. The strategy relies on the base `Strategy.Volume` property for position sizing; the MQL money-management modes are intentionally not replicated.

## Parameter Reference
| Parameter | Description | Default |
|-----------|-------------|---------|
| `CandleType` | Timeframe of the analysed candles. | `H4` |
| `VolumeSource` | Volume stream used for weighting (`Tick` or `Real`; both map to `candle.TotalVolume`). | `Tick` |
| `PriceMethod` / `PriceLength` / `PricePhase` | Primary smoothing algorithm, period, and Jurik phase for the applied price. | `Sma`, `100`, `15` |
| `RangeMethod` / `RangeLength` / `RangePhase` | Smoothing algorithm, period, and phase for the candle range. | `Jjma`, `20`, `100` |
| `ResultLength` | Jurik period applied to the volume-weighted oscillator and the volume stream. | `20` |
| `PriceMode` | Applied-price formula (Close, Open, Median, Demark, TrendFollow0/1, etc.). | `Close` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | Level multipliers retained for visual diagnostics; they do not alter signals. | `0`, `0`, `0`, `0` |
| `SignalBar` | Number of fully closed candles to skip before evaluating the colour flip. | `1` |
| `AllowLongEntries` / `AllowShortEntries` | Permission flags for opening long/short trades. | `true` |
| `AllowLongExits` / `AllowShortExits` | Permission flags for closing existing positions on opposite colour. | `true` |
| `StopLossPoints` / `TakeProfitPoints` | Protective offsets in price points (multiplied by `PriceStep`) passed to `StartProtection`. | `1000`, `2000` |

## Risk Management
- Stop-loss and take-profit levels are translated into `UnitTypes.Point` offsets and managed by `StartProtection`. Set either value to `0` to disable the respective protection.
- The position size is entirely controlled by `Strategy.Volume`; adjust it to match the instrument contract size.
- Colours are evaluated only when the strategy is formed, online, and trading is allowed (`IsFormedAndOnlineAndAllowTrading()` safeguard).

## Limitations & Differences vs. MQL5
- **Smoothing approximations**: Jurik, SMA, EMA, SMMA, LWMA, and KAMA match directly. `Jurx`, `Parma`, and `Vidya` are mapped to the closest StockSharp indicators (ZeroLag EMA, ALMA, EMA respectively), so exotic combinations may deviate slightly from the MT5 output.
- **Volume data**: StockSharp candles expose only total volume; if tick counts are required, feed custom candles where `TotalVolume` represents tick volume.
- **Money management**: Margin modes (`MM`, `MarginMode`) from the original EA are not ported. Use `Volume` or external portfolio management to size trades.
- **Execution timing**: Orders are sent immediately after the signal candle closes, instead of scheduling at the start of the next bar (`TimeShiftSec` in MT5). Behaviour is equivalent for market orders in most setups.

## Usage Notes
1. Attach the strategy to the desired security, configure `Volume`, and ensure `Security.PriceStep` is correct.
2. Choose the candle timeframe via `CandleType`. Multi-timeframe behaviour is not required; the strategy subscribes to a single series.
3. Tune the smoothing methods and lengths to reproduce the indicator behaviour you expect. Use the chart layers (`DrawCandles`, `DrawIndicator`) to verify the oscillator visually.
4. Adjust `SignalBar` if you need faster (0) or more confirmed (≥1) reactions.
5. Start in paper trading to validate risk parameters before deploying to production.

## Optimisation Ideas
- Optimise `PriceLength`, `RangeLength`, and `ResultLength` together to balance responsiveness and noise reduction.
- Test different applied-price formulas (`PriceMode`) to find the variant that tracks your market best.
- Experiment with `SignalBar` delays to smooth whipsaws in choppy conditions.
- Use `HighLevel*` and `LowLevel*` values to build custom visual overlays or filters in analytics dashboards.

## Safety Checklist
- Confirm that the account has permission to trade the selected volume and that the broker enforces comparable tick/real volume semantics.
- Monitor execution latency; the strategy assumes fills close to market price because orders are sent on bar close.
- Keep logs of colour transitions to ensure the smoothing approximations match expectations when upgrading StockSharp or adding new indicators.
