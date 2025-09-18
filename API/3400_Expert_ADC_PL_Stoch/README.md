# Expert ADC PL Stoch Strategy

## Overview

The **Expert ADC PL Stoch Strategy** is a candlestick pattern strategy converted from the original MQL5 expert advisor *Expert_ADC_PL_Stoch*. It looks for bullish Piercing Line and bearish Dark Cloud Cover formations on finished candles and confirms the signals with the %D line of a Stochastic Oscillator. The method is trend-following when the market retraces into an established move and requires the oscillator to be in extreme zones before opening positions. Position exits are based on Stochastic crossovers out of extreme areas, mirroring the vote-based exit logic of the source system.

## Trading Logic

1. Subscribe to a configurable candle type (default: 1-hour time frame).
2. For each finished candle, maintain the last candles needed for candlestick pattern evaluation and the recent Stochastic %D values.
3. **Long Entry**
   - The previous candle pair must form a Piercing Line pattern:
     - Candle at bar *t-1* is bullish with a body greater than the average body size.
     - Candle at bar *t-2* is bearish with a body greater than the average.
     - The bullish candle gaps below the bearish low and closes back inside the bearish body while the overall trend is downward according to the close average.
   - The Stochastic %D value on bar *t-1* must be below the long entry threshold (default 30).
4. **Short Entry**
   - The previous candle pair must form a Dark Cloud Cover pattern:
     - Candle at bar *t-2* is bullish with a large body.
     - Candle at bar *t-1* opens above the previous high and closes back within the bullish body.
     - The mid-price of the bearish candle is above the moving average of closes, signalling an uptrend prior to the reversal.
   - The Stochastic %D on bar *t-1* must be above the short entry threshold (default 70).
5. **Exit Conditions**
   - Long positions are closed when the Stochastic %D on bar *t-1* crosses below either the upper (80) or lower (20) thresholds compared with bar *t-2*.
   - Short positions are closed when the Stochastic %D on bar *t-1* crosses above either the lower (20) or upper (80) thresholds compared with bar *t-2*.
6. All calculations are performed on finished candles; no intrabar processing is used.

## Parameters

| Name | Description | Default |
| ---- | ----------- | ------- |
| `CandleType` | Time frame of candles used for pattern detection. | 1 hour |
| `StochasticLength` | Base length for the Stochastic oscillator. | 47 |
| `StochasticKPeriod` | Smoothing length for the %K line. | 9 |
| `StochasticDPeriod` | Smoothing length for the %D line. | 13 |
| `StochasticSlow` | Additional slowing factor applied to the oscillator. | 3 |
| `AverageBodyPeriod` | Number of candles used to measure the reference body size and close average. | 5 |
| `LongEntryThreshold` | Maximum %D value allowed before entering long trades. | 30 |
| `ShortEntryThreshold` | Minimum %D value required before entering short trades. | 70 |
| `ExitLowerThreshold` | Lower boundary used for exit crossovers. | 20 |
| `ExitUpperThreshold` | Upper boundary used for exit crossovers. | 80 |

## Risk Management

- The strategy sends market orders using the base strategy volume (default 1 contract/lot).
- No automatic protective orders are configured; external risk management or `StartProtection` can be added if needed.
- Only one position is managed at a time; opposite signals close the active position before opening a new one.

## Notes

- Average candle bodies and close averages are computed from historical candles to replicate the MQL5 vote logic closely.
- Stochastic values are stored per finished bar to evaluate the same offsets used in the original expert advisor.
- Trades are opened and closed only when the strategy is fully formed and trading is allowed by the base class checks.
