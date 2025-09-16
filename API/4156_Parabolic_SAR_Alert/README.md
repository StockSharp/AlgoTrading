# Parabolic SAR Alert Strategy

## Overview
This strategy is the StockSharp port of the MetaTrader 4 expert advisor `pSAR_alert.mq4`. The original script only played an alert sound whenever the Parabolic SAR indicator flipped from one side of price to the other. The conversion keeps the same decision logic but turns the alerts into actual market orders, allowing the signal to be traded automatically inside StockSharp.

## Trading Logic
- The strategy subscribes to the configured candle type and runs a Parabolic SAR indicator with the classic acceleration factor (0.02) and maximum acceleration (0.2) by default.
- For every finished candle the strategy compares the Parabolic SAR value with the candle close and also tracks the previous candle context.
- When the previous candle closed below the SAR but the current close is above, the indicator has flipped downward and a long position is opened (or an existing short is reversed).
- When the previous candle closed above the SAR but the current close is below, the indicator has flipped upward and a short position is opened (or an existing long is reversed).
- Trade volume is calculated as the base strategy volume plus the absolute current position, ensuring reversals fully exit the prior trade before entering the new direction.
- `StartProtection()` is executed on start so StockSharp automatically manages unexpected disconnections while positions are open.

## Parameters
| Parameter | Default | Description |
|-----------|---------|-------------|
| `AccelerationFactor` | 0.02 | Initial acceleration step that controls how quickly the Parabolic SAR follows price movements. |
| `MaxAccelerationFactor` | 0.2 | Upper bound for the acceleration step, limiting how aggressively the SAR accelerates during strong trends. |
| `CandleType` | 5 minute time frame | Market data type used for indicator updates; change it to switch between time frames or other candle representations. |

All parameters are exposed through `StrategyParam<T>` so they can be optimized directly in the StockSharp Designer.

## Indicator Workflow
1. Subscribe to the configured candle stream via `SubscribeCandles`.
2. Bind the stream to a `ParabolicSar` indicator so StockSharp updates it automatically.
3. Inside the binding callback compare the current SAR value with the close price and retain the previous SAR/close pair.
4. Detect crossovers by evaluating whether the SAR moved from above to below the close (bullish flip) or from below to above (bearish flip).
5. Execute `BuyMarket` or `SellMarket` accordingly and log descriptive messages for every trade.

## Practical Notes
- Because the strategy only reacts to confirmed candle closes it avoids premature signals that may disappear before the bar finishes.
- The default parameters reproduce the behaviour of the MQL script, but you can adjust them to adapt the sensitivity of the Parabolic SAR.
- Attach the strategy to instruments that trend cleanly; the SAR flip logic performs best when reversals are decisive rather than noisy.
- Chart visualisation is enabled automatically when a chart area is available: candles, the Parabolic SAR indicator and own trades are drawn for quick inspection.

## Files
- `CS/ParabolicSarAlertStrategy.cs` – C# implementation of the strategy.
- `README.md` – This documentation in English.
- `README_cn.md` – Chinese translation of the documentation.
- `README_ru.md` – Russian translation of the documentation.
