# Expert Master EURUSD Strategy

## Overview

The Expert Master EURUSD strategy replicates the MetaTrader 4 Expert Advisor *Expert Master*.  
It evaluates a four-candle pattern on the MACD main and signal lines (fast EMA = 5, slow EMA = 15, signal EMA = 3).  
The algorithm expects the indicator to build momentum in one direction before triggering a breakout entry in the opposite direction.

## Trading Logic

### Long Setup
1. MACD signal line forms a descending sequence on the three previous candles and turns upward on the current candle.
2. MACD main line forms a "V" shape where the current value is above the prior three readings.
3. The previous main-line value is below the configurable lower threshold (default −0.00020).
4. The oldest main-line value is below zero while the current value is above the upper threshold (default 0.00020).

### Short Setup
1. MACD signal line forms an ascending sequence on the three previous candles and turns downward on the current candle.
2. MACD main line forms an inverted "V" where the current value is below the prior three readings.
3. The previous main-line value exceeds the upper threshold (default 0.00020).
4. The oldest main-line value is above zero while the current value drops below the short threshold (default −0.00035).

## Position Management

- **Exit on Momentum Loss:** A long position is closed when the current MACD main value falls below the previous one.  
  Short positions are closed when the current MACD main value rises above the previous one.
- **Trailing Stop:** After price moves by the configured number of points in favor of the trade, a trailing stop is activated.  
  The stop is updated on every finished candle using the candle close minus/plus the trailing distance.  
  If the price retraces to the trailing stop, the strategy exits via a market order.

## Risk Management

- Trade volume defaults to the fixed lot size but can be adjusted dynamically through the **Risk Percent** parameter.  
  When risk sizing is enabled, the strategy risks a fraction of the portfolio value on every entry, mimicking the original EA behaviour.

## Parameters

| Name | Description | Default |
| --- | --- | --- |
| `TrailingPoints` | Trailing stop distance in price points. | 25 |
| `FixedVolume` | Fallback trade volume when risk sizing is unavailable. | 1 |
| `RiskPercent` | Percentage of portfolio value used to size positions. | 0.01 |
| `MacdFastPeriod` | Fast EMA length for the MACD main line. | 5 |
| `MacdSlowPeriod` | Slow EMA length for the MACD main line. | 15 |
| `MacdSignalPeriod` | Signal EMA length for the MACD indicator. | 3 |
| `UpperMacdThreshold` | Positive MACD threshold required for entries. | 0.00020 |
| `LowerMacdThreshold` | Negative MACD threshold used in long signals. | −0.00020 |
| `ShortCurrentThreshold` | Negative MACD threshold applied to the current value for shorts. | −0.00035 |
| `CandleType` | Candle type used for indicator calculations. | 1-minute time frame |

## Notes

- Trade only on finished candles to stay aligned with the high-level StockSharp API.  
- The conversion keeps the original EA logic, including risk-based lot sizing and trailing-stop behaviour, while adding extensive parameterization for easier optimization.
