# MA S.R. Trading Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MA S.R. Trading strategy is a trend-reversal system converted from the original MetaTrader advisor "MA S.R Trading". It monitors the shape of a short simple moving average (SMA) to detect when price momentum bends into a local top or bottom. When the SMA peaks or troughs, the strategy immediately enters in the direction of the turn and protects the position with a stop level anchored at the most recent swing.

Unlike classical crossover systems that compare multiple moving averages with different lengths, this approach analyses the curvature of the same SMA by comparing its value on the three most recent completed candles. A local maximum (`SMA[t-2]` greater than both `SMA[t-1]` and `SMA[t-3]`) signals a bearish reversal and triggers a short entry. A local minimum (`SMA[t-2]` below both neighbours) signals a bullish reversal and opens a long position. Immediately after a signal the strategy stores the extreme price over a configurable lookback window and uses it as a protective stop.

The exit logic mimics the trailing modification from the MQL source. For short trades the stop is set to the highest high inside the lookback window, provided that this level remains above the previous close (otherwise the level is ignored). Long positions use the lowest low under the same rule. If price touches the stored level on subsequent candles, the strategy closes the position at market, effectively emulating the stop-loss update from the original expert.

The system is designed for instruments that exhibit pronounced swing behaviour on intraday to short-term charts. Short SMA periods (default = 5) allow the algorithm to react quickly to micro structure changes, while the stop lookback (default = 5 bars for both highs and lows) controls how aggressively the trailing level follows price. Use tighter windows for scalping environments and wider settings for noisier markets.

Backtests on FX majors and liquid index CFDs show the best performance during ranging periods with frequent oscillations. Trends with smooth pullbacks may require additional filters or volatility confirmation to avoid premature reversals. Consider pairing the strategy with broader market context or time filters when deploying live.

## Details

- **Entry Conditions**
  - **Short**: `SMA[t-1] < SMA[t-2]` AND `SMA[t-3] < SMA[t-2]`. The last finished SMA sample forms a local maximum.
  - **Long**: `SMA[t-1] > SMA[t-2]` AND `SMA[t-3] > SMA[t-2]`. The last finished SMA sample forms a local minimum.
- **Stop Management**
  - **Short**: Stop level = highest high within `HighLookback` candles if the level is above the previous close. Exits when price touches the level.
  - **Long**: Stop level = lowest low within `LowLookback` candles if the level is below the previous close. Exits when price touches the level.
- **Position Rules**: Always flips to the latest signal. When reversing, the strategy closes the existing position and opens the new one in a single market order sized to cover the previous exposure plus the desired volume.
- **Default Parameters**
  - `SmaPeriod` = 5.
  - `HighLookback` = 5.
  - `LowLookback` = 5.
  - `CandleType` = 30-minute timeframe.
  - `TradeVolume` = 1 lot (applied to the `Volume` property on start).
- **Filters**
  - Category: Reversal.
  - Direction: Both long and short.
  - Indicators: Simple Moving Average, Highest/Lowest swing tracker.
  - Stops: Dynamic, swing-based.
  - Timeframe: Intraday to swing.
  - Complexity: Medium.
  - Risk level: Moderate (tight stops but frequent trades).

## Usage Notes

1. Works best on instruments with visible oscillations. Consider disabling trading around major news events to avoid false swings.
2. Optimise the SMA period and lookback windows for the targeted symbol and timeframe. Smaller settings increase sensitivity but also whipsaws.
3. The stop levels are recalculated only when a fresh turning signal appears. If a stop becomes invalid (e.g., high not above the previous close) it is discarded, preventing the strategy from placing protective levels too close to price.
4. Because exits rely on market orders, slippage may occur on fast moves. Combine with broker-side protective orders if the venue supports them.
5. The strategy does not use take-profit targets. To add them, extend the logic in `ProcessCandle` with additional conditions.
