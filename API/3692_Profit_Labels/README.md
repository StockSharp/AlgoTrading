# Profit Labels Strategy

## Overview

The **Profit Labels Strategy** converts the MetaTrader 5 expert advisor *Profit Labels (54352)* to the StockSharp high-level API. The strategy monitors Triple Exponential Moving Average (TEMA) crossovers to open positions and draws profit labels on the chart after a position is closed. When the trend flips upward the algorithm opens a long position, and when the trend flips downward it opens a short position. If an opposite position is still active, the strategy first closes it and prints the realized profit label.

Candles are processed through a `SubscribeCandles` subscription, and the indicator is bound via `Bind` to keep the implementation fully high-level. Finished candles update the TEMA values and trigger trading decisions.

## Trading Rules

1. **Bullish crossover**: when the current TEMA value moves above the previous value while the older readings show a downward slope, the strategy opens a long position if no long is currently active.
2. **Bearish crossover**: when the TEMA turns down in the same manner, it opens a short position if no short is active.
3. **Position reversal**: if an opposite position exists at the moment of a new signal, the strategy closes the open position before placing a new order.
4. **Profit labels**: once the position is fully closed, the realized PnL is calculated and displayed on the chart using `DrawText`.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `CandleType` | `TimeSpan.FromMinutes(1).TimeFrame()` | Time frame used for candle subscription. |
| `TemaPeriod` | `6` | Period of the Triple Exponential Moving Average. |
| `TradeVolume` | `0.1` | Volume submitted with each market order. |
| `PlacingTrade` | `false` | Enables or disables live order placement. |
| `LabelOffset` | `0` | Vertical offset applied to the profit label above the trade price. |

## Notes

- The strategy relies solely on finished candles and does not access indicator buffers directly.
- Protective stop-loss and take-profit levels from the MQL version are not replicated; positions are reversed when an opposite signal arrives.
- Profit labels use the security currency whenever it is available and fall back to raw values otherwise.
