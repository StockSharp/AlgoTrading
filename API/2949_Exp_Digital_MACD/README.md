# Exp Digital MACD Strategy

## Overview
The Exp Digital MACD strategy recreates the behaviour of the original MetaTrader 5 expert advisor "Exp_Digital_MACD" inside the StockSharp framework. The system listens to completed candles from a dedicated timeframe and reacts to the relative position and slope of a MACD style oscillator. Four operating modes reproduce the decision rules of the source code:

1. **Breakdown** – trades zero line transitions of the oscillator.
2. **MACD Twist** – watches for a reversal in the MACD line slope.
3. **Signal Twist** – uses the turn of the signal line itself as confirmation.
4. **MACD Disposition** – looks for the MACD histogram to cross above or below its signal line.

Because StockSharp does not provide the proprietary "Digital MACD" filter, the strategy employs the standard `MovingAverageConvergenceDivergenceSignal` indicator. The defaults (fast EMA 12, slow EMA 26, signal 5) approximate the original setup where the signal smoothing length was equal to five. The strategy processes only finished candles and keeps a short rolling history in private fields to mirror the `SignalBar = 1` behaviour from the MQL implementation.

## Parameters
- **Mode** – selects one of the four trading algorithms described above. Default: `MacdTwist`.
- **FastPeriod** – length of the fast EMA used by MACD. Default: `12`.
- **SlowPeriod** – length of the slow EMA used by MACD. Default: `26`.
- **SignalPeriod** – length of the signal smoothing EMA. Default: `5` to match the original expert advisor.
- **CandleType** – timeframe for the candle subscription. Default: `4h` candles.
- **OrderVolume** – number of contracts or lots submitted on each market order.
- **StopLossPoints / TakeProfitPoints** – protective offsets expressed in security price steps. They are activated when the security exposes a valid `Step` value; set to zero to disable.
- **EnableLongEntry / EnableShortEntry** – toggles that allow or forbid opening of new long or short positions.
- **EnableLongExit / EnableShortExit** – toggles that allow the strategy to close existing positions in the corresponding direction.

## Trading Logic
The algorithm works on the closing value of each candle:

- **Breakdown**: If the MACD value two bars ago was above zero, the strategy optionally closes short positions and opens a long trade when the subsequent bar falls back to or below zero. Conversely, when the MACD two bars ago was below zero the system closes longs and opens shorts if the next bar rises to or above the zero line. This mirrors the contrarian zero-line logic in the expert advisor.
- **MACD Twist**: Tracks three sequential MACD readings. A long signal appears when the line forms a local trough (value[2] > value[1] and value[0] > value[1]). A local peak generates a short signal. Exits follow the opposite twist.
- **Signal Twist**: Applies the same turning-point detection to the signal line buffer.
- **MACD Disposition**: Works with both MACD and signal buffers. If the MACD previously sat above the signal line but the next observation drops back to or below it, the strategy enters long and closes shorts. The opposite transition leads to short entries and long exits.

Every entry uses a market order sized as `OrderVolume + |current position|` so that a reversal closes the existing exposure and establishes a fresh position in a single instruction. Exit signals issue market orders that flatten the open position only.

## Risk Management
`StartProtection` is enabled once the strategy starts. When `StopLossPoints` or `TakeProfitPoints` are set above zero and the security step is known, the corresponding protective orders are configured in absolute price terms. Keeping the parameters at zero disables the automatic protection.

## Implementation Notes
- The strategy evaluates only the most recently completed candle, equivalent to `SignalBar = 1` in the MQL version.
- The StockSharp MACD implementation differs from the proprietary Digital MACD. Users can tune the EMA lengths to better approximate the original behaviour if desired.
- All comments inside the C# source file are provided in English as requested.

## Usage
1. Attach the strategy to a portfolio and a security that supplies the required candle timeframe.
2. Adjust the parameters to match the desired symbol and volatility characteristics.
3. Start the strategy; it will automatically subscribe to the configured candles, process MACD values, and place market orders according to the selected mode.
4. Monitor the logs or optional chart output to follow indicator values and position changes.
