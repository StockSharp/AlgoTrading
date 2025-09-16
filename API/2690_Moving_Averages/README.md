# Moving Averages Strategy

## Overview
The Moving Averages strategy replicates the classic MetaTrader expert that trades crosses of price relative to a shifted simple moving average (SMA). The algorithm processes completed candles only, ensuring that all trading decisions are based on fully formed bars. Position sizing follows a dynamic risk model tied to account equity and adapts to losing streaks, mimicking the original MQL implementation.

## Trading Logic
- A simple moving average is calculated with a configurable period and an additional forward shift measured in completed bars.
- On every finished candle the strategy checks whether the bar opened above the shifted SMA and closed below it (bearish cross) or opened below and closed above (bullish cross).
- The system only handles one position at a time. When a cross occurs against the active position, the position is closed first and no reversal orders are sent on the same bar.
- If no position is open:
  - A bullish cross opens a long position.
  - A bearish cross opens a short position.

## Position Management
- Long positions are closed when a bearish cross occurs.
- Short positions are closed when a bullish cross occurs.
- Trade execution uses market orders on the strategy security.
- Trade history is tracked to calculate the effective entry price so that profit and loss can be measured when the position is closed.

## Risk Management and Position Sizing
- The base order volume is derived from the portfolio equity multiplied by the **Maximum Risk** parameter, divided by the current close price. If equity is unavailable, the strategy falls back to the default strategy volume.
- A **Decrease Factor** parameter reduces the calculated order volume whenever consecutive losing trades are detected. The reduction is proportional to the loss streak, reproducing the adaptive sizing logic of the MQL version.
- Order volume is never negative; when the reduction fully offsets the base amount the trade is skipped.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `MaximumRisk` | Fraction of account equity risked on each trade. | `0.02` |
| `DecreaseFactor` | Divisor used to shrink volume after consecutive losses. | `3` |
| `MovingPeriod` | Period of the SMA used for signals. | `12` |
| `MovingShift` | Number of completed bars used to shift the SMA forward. | `6` |
| `CandleType` | Candle series used for calculations (time frame). | `5m` candles |

## Notes
- The moving average shift is implemented through an internal circular buffer so that the strategy uses the SMA value from several bars ago, just like the MetaTrader indicator shift parameter.
- Orders are only generated when both the SMA and the shifted buffer are fully formed, preventing premature trades during warm-up.
- Logging messages document entries, exits, and trade results to aid debugging and performance analysis.
