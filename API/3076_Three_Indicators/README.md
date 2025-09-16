# Three Indicators Strategy

## Overview
This strategy is a StockSharp conversion of the original **"Three indicators"** MQL5 expert. It evaluates three classical oscillators—MACD, Stochastic Oscillator, and RSI—on every finished candle of the selected timeframe. Only when all filters align does the strategy enter a position, ensuring that each trade follows a consistent multi-indicator confirmation.

## Trading Logic
1. **Candle Direction Filter** – compares the open price of the current finished candle with the open price of the previous one. A higher open favours long trades, a lower open favours shorts.
2. **MACD Slope Filter** – observes the slope of the MACD main line (difference between the current and the previous MACD main value). A falling MACD favours long positions, a rising MACD favours shorts, exactly as in the source expert.
3. **Stochastic Bias Filter** – checks whether the %D value is below or above the 50 midpoint. Values below 50 support longs, values above 50 support shorts.
4. **RSI Bias Filter** – uses the RSI value relative to 50. Values below 50 authorise longs, values above 50 authorise shorts.

Only if **all four filters** support the same direction will the strategy open a new trade. If an opposite signal appears while a position is open, the strategy immediately reverses by sending a single market order that closes the existing exposure and opens the new direction, mirroring the behaviour of the original MQL logic.

## Parameters
| Parameter | Description |
| --- | --- |
| `CandleType` | Timeframe of the candles supplied to the strategy. Default: 1 minute. |
| `TradeVolume` | Volume used when opening a position or reversing to the opposite side. |
| `MacdFastPeriod` | Fast EMA length inside the MACD calculation. |
| `MacdSlowPeriod` | Slow EMA length inside the MACD calculation. |
| `MacdSignalPeriod` | EMA length for the MACD signal line. |
| `MacdPriceType` | Applied price fed to the MACD indicator (Close, Open, High, Low, Median, Typical, Weighted). |
| `StochasticKPeriod` | Lookback period for the %K line. |
| `StochasticDPeriod` | Smoothing period for the %D line. |
| `StochasticSlowing` | Additional smoothing applied to %K before %D is calculated. |
| `RsiPeriod` | Averaging period used by the RSI filter. |
| `RsiPriceType` | Applied price used when feeding the RSI indicator. |

## Indicators
- **MACD (Moving Average Convergence Divergence)** – configured with the user-specified fast, slow, and signal lengths.
- **Stochastic Oscillator** – uses the StockSharp implementation with configurable %K/%D lengths and slowing.
- **Relative Strength Index (RSI)** – provides the final momentum confirmation.

## Behaviour Notes
- The strategy processes only **finished candles**, improving stability compared to the tick-based trigger in the original expert.
- The 30-second pause present in the MQL version is removed; reversals are issued immediately with the combined market order.
- Stochastic smoothing uses StockSharp's default moving average implementation, which corresponds to the standard SMA-based smoothing of the original script.
- Price-source selection for MACD and RSI is provided through the `IndicatorAppliedPrice` enum, matching the options available in MetaTrader (Close, Open, High, Low, Median, Typical, Weighted).

## Risk Management
No stop-loss or take-profit orders are placed automatically. Position management is driven exclusively by the multi-indicator reversal logic. Add external risk controls if required.

## Usage Tips
1. Select the desired instrument and timeframe through `CandleType`.
2. Adjust indicator parameters to suit the market's volatility and signal frequency.
3. Monitor the chart objects added by the strategy (candles plus the three indicators) to validate signal alignment.
4. Combine with external money management if fixed stops or profit targets are required.
