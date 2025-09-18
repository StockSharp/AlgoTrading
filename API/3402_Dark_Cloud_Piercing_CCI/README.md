# Dark Cloud Piercing CCI Strategy

## Overview
This strategy is a StockSharp port of the MetaTrader Expert_ADC_PL_CCI advisor. It scans the price action for Piercing Line and Dark Cloud Cover candlestick reversals and uses the Commodity Channel Index (CCI) as confirmation. Once a valid pattern is detected together with an extreme CCI reading, the strategy opens a market position in the direction of the reversal and later exits when the CCI moves out of its extreme zone.

## Indicators
- **Commodity Channel Index (CCI):** confirms momentum extremes and produces the exit conditions.
- **Average body length (SMA):** measures candle body size to validate “long” candles inside the pattern definition.
- **Average close price (SMA):** acts as a simple trend filter that mirrors the moving average used in the original MQL logic.

## Trading Rules
### Entry
- **Bullish signal (Piercing Line):**
  1. Previous candle must be a long bearish candle that opens above its close.
  2. Latest candle must be a long bullish candle that opens below the previous low and closes within the previous body, above its midpoint but below the prior open.
  3. The midpoint of the older candle has to be below the moving average to confirm a short-term downtrend.
  4. The most recent completed CCI value must be less than or equal to `-EntryConfirmationLevel` (default `50`).
  5. If a short position exists it is fully closed before entering long.
- **Bearish signal (Dark Cloud Cover):** mirrored logic of the bullish signal with a long bullish candle followed by a long bearish candle that gaps up, penetrates the prior body, and closes below its midpoint while CCI is greater than or equal to `EntryConfirmationLevel`.

### Exit
- **Long positions:** closed when the CCI crosses down below `ExitLevel` or crosses down below `-ExitLevel` from above, signalling that momentum has normalised.
- **Short positions:** closed when the CCI crosses up above `-ExitLevel` or above `ExitLevel` from below.

### Position Sizing
- Uses the base `Volume` property. When the signal requires reversing an existing position the strategy automatically adds the absolute size of the current position to the order volume, ensuring a full flip.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle type and timeframe used for detection. | `1H` time frame |
| `CciPeriod` | Lookback length of the Commodity Channel Index. | `49` |
| `AverageBodyPeriod` | Number of candles for the body-size moving average. | `11` |
| `EntryConfirmationLevel` | Absolute CCI level that validates pattern entries. | `50` |
| `ExitLevel` | Absolute CCI level that triggers position exits. | `80` |

## Notes
- The strategy processes only finished candles and ignores partial updates.
- No stop-loss or take-profit orders are set automatically; exits are purely signal based as in the original expert advisor.
- Ensure the instrument has a price step configured because the equality tolerance of the candlestick logic depends on the security settings.
