# BW WiseMan-1 Breakout Strategy

This strategy is a StockSharp port of the MetaTrader expert advisor **Exp_BW-wiseMan-1**. It automates Bill Williams' WiseMan-1 breakout logic built around the Alligator indicator. Signals are produced whenever a completed candle escapes from the Alligator's jaws and simultaneously breaks the most recent swing extremes. Optional counter-trend mode swaps the signals so that the strategy can fade the same breakouts.

## Core Idea
- Compute the Bill Williams Alligator using smoothed moving averages of the median price (high + low) / 2.
- Shift the jaw, teeth and lips lines forward by configurable offsets to match the original indicator's visualization.
- Confirm a breakout only when the current candle expands beyond the highs or lows of the last *N* bars, ensuring that the move is stronger than recent noise.
- Delay execution by the selected number of completed candles so the trader can operate on older signals if desired.

## Trading Rules
### Long direction
1. The bar must finish **below** all three Alligator lines (high price less than jaw, teeth and lips).
2. The close price needs to be in the upper half of the candle, i.e., above the candle's median.
3. The low of the candle must be strictly lower than the lows of the previous `Back` bars.
4. When the signal becomes active after the `SignalBar` delay:
   - Close any open short if `Close Short` is enabled.
   - Open a new long position if `Enable Long` is enabled and no position is currently open.

### Short direction
1. The bar must finish **above** all three Alligator lines (low price greater than jaw, teeth and lips).
2. The close price must be in the lower half of the candle, i.e., below the candle's median.
3. The high of the candle has to be greater than the highs of the previous `Back` bars.
4. When the signal becomes active:
   - Close any existing long if `Close Long` is enabled.
   - Open a new short position if `Enable Short` is enabled and there is no current position.

### Counter-trend mode
Setting `Counter-Trend Mode` to **true** swaps the buy and sell signals so that the strategy takes trades against the Alligator breakout direction.

## Parameters
- **Candle Type** – timeframe used to build candles and calculate all indicator values (default: 1 hour).
- **Counter-Trend Mode** – invert the breakout logic to trade against the primary trend (default: enabled, matching the original EA).
- **Breakout Depth (`Back`)** – number of prior bars compared against the current high/low when validating a breakout (default: 2).
- **Jaw Length / Shift** – smoothed moving average length and forward displacement for the jaw line (defaults: 13 / 8).
- **Teeth Length / Shift** – smoothed moving average length and forward displacement for the teeth line (defaults: 8 / 5).
- **Lips Length / Shift** – smoothed moving average length and forward displacement for the lips line (defaults: 5 / 3).
- **Signal Bar** – number of already finished candles to wait before executing a detected signal (default: 1).
- **Enable Long / Enable Short** – toggles for opening new long or short positions.
- **Close Long / Close Short** – toggles for closing opposite positions when the signal fires.

## Notes
- The strategy relies solely on market orders and does not set hard stop-loss or take-profit levels. Any exit is driven by the opposite signal or by disabling the relevant close toggle.
- All calculations are performed on finished candles; partial intrabar data is ignored to stay consistent with the source MetaTrader expert.
- Volume is inherited from the StockSharp strategy settings. Adjust the base volume in the platform configuration if you need a different position size.
