# Weight Oscillator Direct Strategy

## Overview
This strategy reproduces the MetaTrader expert **Exp_WeightOscillator_Direct** inside the StockSharp high-level API. It blends four classic oscillators—RSI, Money Flow Index, Williams %R and DeMarker—into a single weighted composite. The composite signal is smoothed by a configurable moving average and used to detect momentum swings. A rising composite opens long trades (or closes shorts) when the strategy works in the "Direct" mode, while the "Against" mode inverts the logic for contrarian trading.

## Indicator pipeline
1. **Relative Strength Index (RSI)** – normalized 0..100 scale.
2. **Money Flow Index (MFI)** – liquidity-sensitive oscillator in 0..100 range.
3. **Williams %R (WPR)** – shifted by +100 to align with the 0..100 scale.
4. **DeMarker** – multiplied by 100 to match the other oscillators.
5. **Smoothing average** – one of the supported moving averages (Simple, Exponential, Smoothed, Weighted, Jurik, Kaufman).
6. **Composite oscillator** – weighted average of the normalized inputs, smoothed to remove noise.

The weighted oscillator value is stored for each finished candle. Signals analyse the last three stored values, optionally skipping a number of most recent bars via the *Signal Bar* parameter to mimic the original expert behaviour.

## Trading logic
1. Wait until all indicators and the smoothing moving average are fully formed.
2. Compute the smoothed composite oscillator for the current finished bar and append it to history.
3. Retrieve three historical values: `current`, `previous`, `prior`, with indices controlled by *Signal Bar*.
4. Detect slope changes:
   - **Rising** when `previous < prior` **and** `current > previous`.
   - **Falling** when `previous > prior` **and** `current < previous`.
5. Depending on the selected *Trend Mode*:
   - **Direct**: trade with the slope (`rising` → long signal, `falling` → short signal).
   - **Against**: trade against the slope (`rising` → short, `falling` → long).
6. Apply the entry/exit switches:
   - Close opposite exposure if the corresponding *Close* switch is enabled.
   - Open new positions only if the respective *Allow* switch is enabled. Order size equals `Volume + |Position|` so the strategy can flip from short to long (or vice versa) in a single market order.
7. Optional stop-loss and take-profit protections are activated through `StartProtection` using distances expressed in price steps.

## Parameters
| Group | Name | Description |
|-------|------|-------------|
| General | **Candle Type** | Timeframe for data subscription and indicator calculations. |
| Trading | **Trend Mode** | `Direct` follows the oscillator slope, `Against` trades counter-trend. |
| Trading | **Signal Bar** | Number of latest closed bars to skip (1 = last closed bar). |
| Oscillator | **RSI / MFI / WPR / DeMarker Weight** | Relative contribution of each oscillator in the weighted blend. Zero disables a component. |
| Oscillator | **RSI / MFI / WPR / DeMarker Period** | Lookback length for each oscillator. |
| Oscillator | **Smoothing Method** | Moving average applied to the composite (Simple, Exponential, Smoothed, Weighted, Jurik, Kaufman). |
| Oscillator | **Smoothing Length** | Period for the smoothing average. |
| Risk Management | **Stop Loss Points** | Distance in price steps; `0` disables the stop. |
| Risk Management | **Take Profit Points** | Distance in price steps; `0` disables the target. |
| Trading | **Allow Long/Short Entries** | Enable or disable opening new long/short positions. |
| Trading | **Close Shorts/Longs on Signal** | Allow closing existing exposure when an opposite signal arrives. |

All numeric parameters are exposed as `StrategyParam` objects, allowing optimisation inside the StockSharp Designer.

## Usage notes
- Set the base `Volume` property before starting the strategy. Market orders will scale automatically when reversing positions.
- The strategy subscribes to exactly one candle series returned by `GetWorkingSecurities()`.
- Protective stops use instrument `PriceStep` to convert point distances into absolute price values.
- When *Trend Mode* is set to `Against`, only the signal polarity changes; all other mechanics remain identical to the original expert advisor.
- Williams %R and DeMarker are normalized to share the same 0..100 scale as RSI/MFI, matching the original indicator logic.

## Differences from the MQL expert
- The original indicator supported additional smoothing types (`ParMA`, `JurX`, `VIDYA`, `T3`). In StockSharp the strategy offers high-quality counterparts (Jurik and Kaufman) while defaulting to Jurik for compatibility.
- Money Flow Index always uses the candle's aggregated volume. MetaTrader could switch between tick and real volumes; this choice is data-source dependent in StockSharp.
- Risk management is implemented through `StartProtection` (price-step based) instead of point-based requests, but it delivers the same behaviour when `PriceStep` matches the instrument contract size.

## Getting started
1. Attach the strategy to a portfolio and security that support the configured candle type.
2. Adjust indicator weights/periods and enable or disable entry switches.
3. Choose the smoothing method and length that best fit the instrument's volatility.
4. Configure stop-loss/take-profit distances in price steps if protection is required.
5. Run the strategy; signals will only execute on finished candles, ensuring deterministic behaviour.
