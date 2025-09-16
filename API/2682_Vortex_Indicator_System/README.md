# Vortex Indicator System Strategy

## Overview
- **Source**: Converted from the MetaTrader 5 expert advisor "Vortex Indicator System" (MQL ID 19137).
- **Concept**: Uses the Vortex indicator to detect bullish or bearish crossovers and then arms breakout triggers on the high/low of the crossover candle.
- **Execution Style**: Breakout following; trades are initiated only after price confirms the crossover by exceeding the trigger level.
- **Market Regime**: Works on any instrument and timeframe that supports the Vortex indicator and candle data; no broker-specific features are required.
- **Order Types**: Market orders via `BuyMarket` and `SellMarket`. The strategy automatically closes opposite positions before queuing a new trigger.

## Trading Logic
1. Subscribe to the configured candle type and calculate the Vortex indicator with the specified length.
2. Detect a bullish crossover when `VI+` moves above `VI-` after being below it on the previous candle:
   - Close any existing short position using `ClosePosition()`.
   - Store the high of the crossover candle as the long trigger price.
   - Cancel any pending short trigger.
3. Detect a bearish crossover when `VI-` moves above `VI+` after being below it on the previous candle:
   - Close any existing long position.
   - Store the low of the crossover candle as the short trigger price.
   - Cancel any pending long trigger.
4. While a trigger is active, monitor subsequent candles:
   - If the high price breaks the stored long trigger and the current position is flat or short, send a market buy sized to reverse any short exposure.
   - If the low price breaks the stored short trigger and the current position is flat or long, send a market sell sized to reverse any long exposure.
5. Each executed trade clears its corresponding trigger. Opposite triggers are mutually exclusive.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `Length` | 14 | Period of the Vortex indicator. Matches the original MQL input `VI_Length`. |
| `CandleType` | 60-minute timeframe | Candle type used for indicator calculation and trigger evaluation. Can be adjusted to any timeframe supported by the connected data source. |
| `Volume` | Taken from the base `Strategy` property | Trade volume used for market orders. Configure it before starting the strategy if a value different from 1 contract/lot is required. |

### How parameters affect behaviour
- Increasing `Length` smooths the Vortex lines, reducing the number of crossovers but improving their reliability.
- Decreasing `Length` makes the system more reactive, generating more triggers and potential trades.
- The `CandleType` should be aligned with the data granularity in the original MQL setup (typically the chart timeframe). Shorter candles provide faster signals, whereas longer candles focus on broader trends.

## Risk Management Notes
- The original expert advisor does not define stop loss or take profit levels. This conversion keeps that behaviour; risk management must be handled externally or by extending the strategy.
- Position reversal is immediate: when an opposite signal occurs, the strategy issues `ClosePosition()` and waits for a breakout beyond the trigger before entering in the new direction.
- Only one trigger (long or short) can be active at a time. Triggers are cleared if price breaks them or when an opposite crossover occurs.

## Usage Instructions
1. Add the strategy to your StockSharp project and ensure the `StockSharp.Algo.Indicators` package is available.
2. Configure the desired security and connector in the host application.
3. Set the `CandleType` parameter to the timeframe you want to trade. It should correspond to an available candle subscription for the selected instrument.
4. Optionally adjust `Length` and `Volume` before starting the strategy or through optimization.
5. Start the strategy. Orders will be generated once the indicator is formed and real-time data is available.

## Implementation Highlights
- Uses the high-level `SubscribeCandles` API with indicator binding (`Bind`) for clean event-driven processing.
- Stores the previous Vortex values to detect crossovers exactly as the MQL implementation does (`VI+` and `VI-` comparisons across two consecutive candles).
- Entry triggers are implemented as nullable decimal fields to mimic the original "arm and break" mechanism.
- English inline comments in the C# file describe each decision step and help maintain the code.

## Possible Extensions
- Add stop loss and take profit rules (e.g., ATR-based exits) if tighter risk control is required.
- Introduce a cooldown period or maximum holding time to avoid prolonged flat periods when triggers do not execute.
- Combine with a volatility filter to trade only when price ranges are wide enough to justify breakout attempts.
