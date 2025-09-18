# Crossover MA Strategy

## Overview

This strategy is a StockSharp port of the MetaTrader 5 expert advisor **CrossoverMA.mq5**. The original robot waits for a candle to cross a moving average and only opens a position when the average is sloping in the same direction as the breakout. The StockSharp version keeps the same behaviour while taking advantage of the high-level API for candle subscriptions, indicator management, and automatic chart rendering.

## Trading Logic

1. Subscribe to the configured candle series and calculate a simple moving average (SMA) over the candle close price.
2. When a finished candle is received, measure:
   - The candle open and close distances from the SMA.
   - The slope of the SMA by comparing the current value with the previous one.
3. Generate signals:
   - **Bullish breakout** – the candle opens below the SMA, closes above it, and the SMA is rising. The strategy closes any short exposure and opens/extends a long position.
   - **Bearish breakout** – the candle opens above the SMA, closes below it, and the SMA is falling. The strategy closes any long exposure and opens/extends a short position.
4. Ignore duplicate signals that do not change the current position side.

The port keeps the MetaTrader rule that only finished candles are processed and that one extra candle is required before the first trade (to measure the SMA slope).

## Parameters

| Name | Description | Default | Notes |
| ---- | ----------- | ------- | ----- |
| `Candle Type` | Time frame used to build candles. | 1 minute time frame | Any StockSharp-supported candle data type can be selected. |
| `MA Length` | Number of completed candles included in the SMA. | 12 | Matches the default period of the MetaTrader expert. |
| `Trade Volume` | Market order volume for entries. | 1 | The strategy closes the opposite exposure before opening a new position. |

All parameters are available for optimisation in StockSharp Designer or Runner.

## Implementation Notes

- The strategy relies on `SubscribeCandles` and `Bind` so indicator values are streamed directly into the processing method without manual history management.
- The SMA is stored in a private field to draw it on the chart area when one is available.
- Signals are processed only when `IsFormedAndOnlineAndAllowTrading()` returns `true`, ensuring the strategy respects the global trading state.
- Position reversals follow the MetaTrader template: close the current exposure first, then open the new side with the configured trade volume.

## Files

- `CS/CrossoverMaStrategy.cs` – C# implementation of the converted strategy.
- `README.md` – English documentation.
- `README_cn.md` – Chinese documentation.
- `README_ru.md` – Russian documentation.

## Porting Differences

- Money-management, trailing-stop, and other MetaTrader framework classes are omitted because StockSharp manages position sizing and risk externally. The `Trade Volume` parameter replaces the fixed lot settings from the original expert.
- MetaTrader used separate data series for candle open and close prices. StockSharp candles already include both prices, so no extra indicators are required.
- Indicator initialisation, validation, and lifecycle management are handled automatically by StockSharp, removing the lengthy boilerplate from the MQL version.
