# Exp UltraFATL Duplex Strategy

## Overview
The **Exp UltraFATL Duplex Strategy** is a C# conversion of the MetaTrader 5 expert advisor `Exp_UltraFatl_Duplex`. The system runs two independent UltraFATL indicator pipelines: one dedicated to long opportunities and another tuned for short setups. Each pipeline evaluates a ladder of smoothed FATL values and counts how many stages are rising or falling. The balance between the bullish and bearish counters defines the direction of the next trade.

## Trading Logic
1. Subscribe to the configured candle timeframe for each directional block.
2. Filter the applied price with the FATL kernel (39-tap digital filter).
3. Feed the filtered series through a ladder of moving averages whose lengths increase by the configured step. The ladder uses the smoothing method specified by the user.
4. Compare consecutive values inside the ladder to count bullish and bearish votes. Smooth both counters with a second moving average.
5. Evaluate the counters at the selected signal shift (default: one fully closed candle):
   - **Long block** opens a position when the previous candle showed bullish dominance, but the current candle shows counters crossing downward (bulls ≤ bears). It closes the long position when bears outnumber bulls on the previous candle.
   - **Short block** works in the opposite direction: it opens a short when the previous candle is bearish dominated and the current candle crosses upward (bulls ≥ bears). It closes the short when bulls lead on the previous candle.
6. Optional stop-loss and take-profit levels are evaluated on candle data using the instrument price step.

The strategy enforces a net position: short signals close existing longs before opening, and vice versa. Market orders are used for entries and exits.

## Parameters
### Long Block
- **Long Volume** – order size when opening a long trade.
- **Allow Long Entries** – enable or disable new long positions.
- **Allow Long Exits** – allow closing longs on opposing signals.
- **Long Candle Type** – timeframe used for the long UltraFATL pipeline.
- **Long Applied Price** – price source (close, typical, DeMark, etc.) fed into the FATL kernel.
- **Long Trend Method / Start Length / Phase / Step / Steps** – ladder smoothing configuration.
- **Long Counter Method / Counter Length / Counter Phase** – smoothing settings for the bullish/bearish counters.
- **Long Signal Bar** – number of completed candles used as the signal offset (values below 1 are treated as 1).
- **Long Stop (pts)** – optional stop-loss distance in price steps.
- **Long Target (pts)** – optional take-profit distance in price steps.

### Short Block
Symmetric settings for the short pipeline: **Short Volume**, **Allow Short Entries**, **Allow Short Exits**, **Short Candle Type**, **Short Applied Price**, **Short Trend Method / Start Length / Phase / Step / Steps**, **Short Counter Method / Counter Length / Counter Phase**, **Short Signal Bar**, **Short Stop (pts)**, **Short Target (pts)**.

## Implementation Notes
- The smoothing methods map to StockSharp indicators. Jurik-based options reuse `JurikMovingAverage`; methods such as `Parabolic` and `T3` are approximated with exponential or Jurik moving averages because the original custom kernels are not available.
- Stop-loss and take-profit levels are evaluated on candle highs/lows; they are not server-side protective orders.
- Signal offsets below one bar cannot be reproduced because the StockSharp port reacts to finished candles only. Setting the signal bar to zero therefore behaves identically to a shift of one.
- Both indicator pipelines draw their smoothed counters on dedicated chart areas for visual inspection.

## Usage
Add the strategy to your StockSharp solution, configure the directional blocks according to your trading plan, and run it inside the Designer, Shell, or Runner. Ensure that the instrument provides the required candle series and that the `LongVolume`/`ShortVolume` parameters are set to the desired order size.
