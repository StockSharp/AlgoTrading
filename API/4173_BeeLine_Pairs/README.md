# BeeLine Pairs Strategy

The **BeeLine Pairs Strategy** is a mean-reversion system converted from the original MQL5 expert advisor. It trades two correlated instruments by continuously recalibrating the price compression between them and entering when their synthetic spread diverges beyond a dynamic threshold. Instead of opening two legs, the strategy can optionally execute a single cross instrument that represents the pair.

## Key Concepts

- **Price compression** – recalculates the ratio between instruments using recent highs and lows.
- **Deviation tracking** – measures the spread in points of the main instrument and scales the entry volume by the number of standard deviations.
- **Cross execution** – when a cross security is supplied the strategy flips or keeps the signal depending on the `UseDirectCrossRate` flag.

## Parameters

| Parameter | Description | Default |
| --- | --- | --- |
| `SecondSecurity` | Secondary instrument paired with the main security. Must be assigned before start. | `None` |
| `CrossSecurity` | Optional cross instrument traded instead of two separate legs. | `None` |
| `UseDirectCrossRate` | Use the cross signal directly (`true`) or invert it (`false`). | `true` |
| `TrainingRange` | Number of finished candles used to estimate compression and signal width. | `640` |
| `ProfitPercent` | Close all positions when the portfolio gain reaches this percentage. | `3` |
| `SignalCorrection` | Multiplier applied to the detected maximum deviation to tighten the signal band. | `0.7` |
| `DistanceMultiplier` | Multiplier defining how many historical bars participate in the signal width search. | `1.2` |
| `RetrainInterval` | Number of finished candles between compression recalculations. | `120` |
| `MaxDeals` | Maximum scaling factor applied to the base trade volume. | `3` |
| `CloseCorrection` | Exit when deviation shrinks below this fraction of the signal width. | `0.618034` |
| `Correlation` | Relationship between instruments (`1` for positive, `-1` for inverse). | `1` |
| `CandleType` | Timeframe used for the calculations. | `5 minute candles` |

## Trading Logic

1. Subscribe to the selected candle series for both instruments and align candles by open time.
2. After enough data arrives, recompute the compression coefficient and maximum deviation window. The highest absolute deviation multiplied by `SignalCorrection` defines the current signal border.
3. Record the deviation of each finished bar. A trading signal appears when:
   - The absolute deviation exceeds the signal border.
   - The deviation started to contract compared with the previous bar (mean reversion begins).
   - The strategy has volume budget left up to `MaxDeals` multiples of the base lot.
4. When a signal is active:
   - **Positive correlation (`Correlation = 1`)** – sell the main instrument and buy the secondary when the main instrument is overpriced (positive deviation), otherwise open the opposite combination.
   - **Negative correlation (`Correlation = -1`)** – trade both legs in the same direction to exploit inverse co-movement.
   - **Cross security configured** – trade the cross instrument, optionally inverting the signal by `UseDirectCrossRate`.
5. Close positions when any of the following occur:
   - The deviation crosses zero according to the correlation rules above.
   - The deviation contracts below `CloseCorrection * MaxDeviation`.
   - The account equity gain reaches `ProfitPercent` of the starting capital.

## Practical Notes

- Ensure both instruments provide valid `PriceStep`, `StepPrice`, and volume step settings; they are required for proper normalization of deviation and order sizes.
- The strategy stores the most recent deviations to rebuild the signal band after each optimisation. It automatically trims the history to the configured window.
- When trading a cross instrument the secondary security positions stay flat; make sure the cross symbol is mapped correctly on the connector side.
- Volumes are normalised to the exchange constraints of each instrument before sending market orders.
