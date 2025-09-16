# JS MA Day Strategy

## Overview

The **JS MA Day Strategy** trades based on a simple moving average calculated on daily candles using the median price. The strategy compares the position of the moving average relative to each day's open price and opens positions when the trend of the moving average confirms a crossover of the open price.

## Indicators

- Simple Moving Average (median price)

## Parameters

| Name | Description | Default |
|------|-------------|---------|
| `MaPeriod` | Period of the simple moving average. | `3` |
| `Reverse` | Reverse trading signals. When enabled, buy signals become sell signals and vice versa. | `false` |
| `CandleType` | Candle type used for calculations. Default is daily timeframe candles. | `TimeFrame(1 day)` |

## Entry Rules

1. Evaluate the daily simple moving average (SMA) and daily open prices.
2. **Buy** when:
   - Current SMA is below the previous SMA.
   - Current SMA is above today's open price.
   - Previous SMA is below the SMA two days ago.
   - Previous SMA is above the previous day's open price.
3. **Sell** when:
   - Current SMA is above the previous SMA.
   - Current SMA is below today's open price.
   - Previous SMA is above the SMA two days ago.
   - Previous SMA is below the previous day's open price.
4. If `Reverse` is enabled, buy and sell conditions are swapped.

## Exit Rules

- Positions are closed by calling `StartProtection`, which allows configuring protective orders such as stop loss or take profit through the platform settings.

## Notes

- The strategy processes only completed candles.
- The volume of orders is defined by the `Volume` property of the base class.
- There is no Python version of this strategy yet.

