# BladeRunner Strategy

## Overview
The BladeRunner strategy is a translation of the MetaTrader expert advisor that combines fractal breakouts with trend and momentum confirmation. The StockSharp port keeps the multi-timeframe structure of the original script by analysing three different candle feeds: a primary series for trade execution, a higher timeframe series for the momentum filter, and a slow series for the MACD trend filter. Orders are opened with configurable scaling, stop-loss and take-profit distances expressed in price steps.

## Trading Logic
1. **Fractal breakout filter** – the strategy scans completed candles for Bill Williams fractal patterns. A bullish (upper) fractal is accepted when the candle formed two bars earlier makes a new swing high and the confirmation bar opens below both the fractal price and the 20-period LWMA of the typical price. Bearish fractals apply the symmetrical rules.
2. **Trend confirmation** – fast and slow linear weighted moving averages (LWMA) calculated on the primary candle series define the underlying trend. Longs require the fast LWMA to be above the slow LWMA, while shorts demand the opposite alignment.
3. **Momentum filter** – a momentum oscillator computed on the higher timeframe candle stream must deviate from 100 by at least the configured threshold in any of the latest three observations. This reproduces the momentum spike checks from the MQL version.
4. **MACD filter** – a MACD calculated on the slow timeframe must have its main line above (long) or below (short) the signal line, mirroring the monthly filter used by the expert advisor.
5. **Breakout confirmation** – the close of the most recent primary candle has to break beyond the stored fractal level before the order is sent.

Whenever all filters align, the strategy opens a market position using the configured lot size. Existing exposure in the opposite direction is closed before reversing. Additional entries are allowed until the maximum number of scale-in trades is reached.

## Implementation Details
- Three candle subscriptions are created via the high-level API. Each feed binds directly to the required indicators without adding them to the global indicator collection.
- LWMAs operate on the typical price (HLC/3) to match the MQL implementation. The MACD also consumes typical prices.
- Fractal detection stores a sliding window of completed candles and associated filter values. Only the most recent validated fractal direction is kept, which prevents duplicate signals on the same structure.
- Momentum history is maintained as a fixed-size array, avoiding dynamic allocations while reproducing the look-back of the original EA.
- Order sizing honours exchange constraints through volume step, minimum and maximum volume adjustments.
- The built-in `StartProtection` helper applies stop-loss and take-profit distances expressed in price steps, matching the fixed pip values from MetaTrader.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Primary candle series used for signal generation. | 15-minute candles |
| `MomentumCandleType` | Higher timeframe series for the momentum filter. | 1-hour candles |
| `MacdCandleType` | Candle series used by the MACD trend filter. | Daily candles |
| `FastMaPeriod` | Length of the fast LWMA. | 6 |
| `SlowMaPeriod` | Length of the slow LWMA. | 85 |
| `FilterMaPeriod` | LWMA used to validate fractal breakouts. | 20 |
| `MomentumPeriod` | Momentum indicator averaging period. | 14 |
| `MomentumThreshold` | Minimum absolute deviation of momentum from 100. | 0.3 |
| `FractalLookback` | Number of candles retained for fractal analysis. | 200 |
| `MaxTrades` | Maximum number of scale-in orders per direction. | 3 |
| `OrderVolume` | Base volume for each market order. | 1 contract |
| `TakeProfitSteps` | Take-profit distance expressed in price steps. | 50 |
| `StopLossSteps` | Stop-loss distance expressed in price steps. | 20 |

## Risk Management
- Stop-loss and take-profit levels are attached automatically to every position through `StartProtection`.
- The strategy always closes opposing exposure before opening trades in the new direction to avoid hedged situations.
- Volume is adjusted to the instrument constraints before placing orders. The `MaxTrades` limit caps the total scaling steps per direction.

## Differences from the Original EA
- MetaTrader’s equity stop, trailing stop and break-even utilities are not implemented. StockSharp risk control can be added externally if needed.
- Money-based trailing logic and push notifications are omitted because StockSharp provides alternative notification workflows.
- The MACD filter uses daily candles by default instead of monthly bars. Adjust `MacdCandleType` to a monthly timeframe when it is supported by the connected data source.
- Fractal validation relies on the latest confirmation candle stored in the sliding window. This produces the same practical effect as the loop in the MQL script while avoiding repeated scans.

## Usage Notes
1. Configure the candle types to match the instruments and timeframes supported by your data source.
2. Align `OrderVolume`, `TakeProfitSteps`, and `StopLossSteps` with the instrument’s tick size and volume step.
3. Tune `MomentumThreshold` and the LWMA lengths during walk-forward tests to adapt the breakout sensitivity to different markets.
4. Enable chart drawing to visualise the three LWMAs and verify that fractal breakouts align with the trend filters before running live.
