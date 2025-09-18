# CDC PL RSI Strategy

## Overview
The **CDC PL RSI Strategy** replicates the MQL Expert Advisor *Expert_ADC_PL_RSI* inside the StockSharp ecosystem. The system scans finished candles for Japanese candlestick reversal patterns and confirms entries with the Relative Strength Index (RSI). Long trades rely on the *Piercing Line* pattern during oversold RSI conditions, while short trades require the *Dark Cloud Cover* pattern combined with overbought RSI readings. The approach keeps the original money-management concept simple by using the strategy volume and letting StockSharp handle position sizing.

## Pattern and Indicator Logic
- **Candlestick patterns**: The strategy reconstructs the MetaTrader logic by analysing two latest finished candles. Piercing Line and Dark Cloud Cover rules mirror the original code, including checks for gaps, long bodies relative to an adaptive body average and the underlying trend direction.
- **RSI filter**: A 20-period RSI (optimizable) confirms momentum. Oversold readings (`RSI < 40`) unlock long entries, and overbought readings (`RSI > 60`) unlock shorts. RSI history is also used to detect exits when the oscillator crosses the 30 or 70 levels in the opposite direction.
- **Body average and trend filter**: A simple moving average of candle body sizes and another SMA of close prices replicate the MetaTrader helper functions (`AvgBody` and `CloseAvg`). These averages prevent signals during noise and enforce that the patterns appear after a clear move.

## Trading Rules
### Long setup
1. Detect a Piercing Line pattern on the last two completed candles.
2. Require RSI from the previous finished candle to be below 40.
3. If conditions hold, buy at market. When an opposite position exists, the strategy reverses by buying the absolute position size plus the configured volume.

### Short setup
1. Detect a Dark Cloud Cover pattern on the two latest candles.
2. Require RSI from the previous finished candle to be above 60.
3. If conditions hold, sell at market. An opposite position is closed and reversed using the same volume logic.

### Exit conditions
- Close long positions when RSI crosses down through 70 or crosses up through 30, signalling that momentum has faded or reverted.
- Close short positions when RSI crosses up through 30 or crosses down through 70, mirroring the MetaTrader implementation.

## Parameters
| Name | Default | Description |
|------|---------|-------------|
| `RsiPeriod` | 20 | RSI look-back length. Optimizable between 10 and 40 in steps of 5. |
| `BodyAveragePeriod` | 14 | Period for both the average candle body size and close-price trend filter. Optimizable between 10 and 30 in steps of 5. |
| `CandleType` | 1-hour time frame | Candle series used for calculations. Any StockSharp-supported candle type can be selected. |
| `Volume` (base class) | — | Trade volume set on the strategy instance before launching. |

## Usage
1. Attach the strategy to a portfolio and security in StockSharp Designer, Shell or Runner.
2. Configure the candle type and volume according to the market being traded.
3. Optionally adjust the RSI and body average periods to match the instrument volatility or perform optimisations using StockSharp Optimizer.
4. Start the strategy and monitor the chart overlays (candles, RSI and close-average line) to review pattern confirmations and executed trades.

## Notes
- The strategy calls `StartProtection()` so that built-in protective routines can be configured if required (stop-loss, take-profit, trailing, etc.).
- Only completed candles are processed, keeping the logic consistent with the MQL Expert Advisor.
- No additional collections are stored; indicator instances carry the sliding-window computations required for the pattern checks.
