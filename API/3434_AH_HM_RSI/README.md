# AH HM RSI Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader expert **Expert_AH_HM_RSI**. It looks for hammer or hanging man candlestick patterns and requires a confirming signal from the Relative Strength Index (RSI) before trading. The approach mirrors the original Expert Advisor, including its risk management philosophy of reversing positions when a fresh signal appears.

## Trading Logic
1. **Trend Filter** – A short Simple Moving Average (default length 2) is used to determine whether the market is in a micro downtrend or uptrend.
2. **Candlestick Pattern** – The strategy analyses the most recent completed candle:
   - A **hammer** is detected when the body sits in the upper third of the range, the candle gaps lower than the previous bar, and the midpoint of the candle is below the moving-average trend.
   - A **hanging man** is detected when the body sits in the upper third, the candle gaps higher than the previous bar, and the midpoint of the candle is above the moving-average trend.
3. **RSI Filter** –
   - Long trades require the RSI to be below the configurable hammer threshold (default 40).
   - Short trades require the RSI to be above the hanging-man threshold (default 60).
4. **Trade Execution** – On a valid signal the strategy enters with `Volume + |Position|`, so open positions are reversed immediately when the opposite setup arrives.
5. **Exit Rules** – Positions are flattened when the RSI crosses the configurable lower (default 30) or upper (default 70) boundaries in the opposite direction, replicating the exit votes in the original code.

## Indicators
- **RelativeStrengthIndex** (length 33 by default).
- **SimpleMovingAverage** (length 2 by default) applied to candle closes.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `Volume` | Order size used for entries. | `1` |
| `RsiPeriod` | RSI lookback period. | `33` |
| `MaPeriod` | Moving-average period for the trend filter. | `2` |
| `HammerRsiThreshold` | Maximum RSI value that allows a hammer long entry. | `40` |
| `HangingManRsiThreshold` | Minimum RSI value that allows a hanging-man short entry. | `60` |
| `LowerExitLevel` | RSI boundary used to close shorts after an upward cross. | `30` |
| `UpperExitLevel` | RSI boundary used to close longs after a downward cross. | `70` |
| `CandleType` | Timeframe processed by the strategy. | `1 hour` candles |

All parameters can be optimised via the StockSharp parameter UI.

## Usage Notes
- The logic works exclusively on finished candles. Ensure the selected timeframe and data feed produce complete bars.
- Because the reversal logic always trades `Volume + |Position|`, positions flip direction instantly on the opposite signal, matching the Expert Advisor.
- Start the built-in risk management once at launch (`StartProtection()` is called in `OnStarted`).

## Files
- `CS/AhHmRsiStrategy.cs` – Strategy implementation.
- `README.md` – English documentation.
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.
