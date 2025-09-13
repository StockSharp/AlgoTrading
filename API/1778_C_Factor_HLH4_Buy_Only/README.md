# C Factor HLH4 Buy Only Strategy

This strategy is a C# translation of the original MQL expert advisor **C_Factor_HLH4_buy_only**. It demonstrates how to port MetaTrader strategies to the StockSharp high-level API.

## Strategy Logic

- Uses four-hour time frame candles.
- Opens a long position when the current candle closes above the previous candle's high.
- Exits the long position when the close price:
  - exceeds the previous candle's low by 100 ticks, or
  - falls below the previous candle's high by 20 ticks.
- Risk management is handled with configurable stop-loss and take-profit distances.
- Order volume is calculated from the percentage of account equity risked per trade.

## Parameters

| Name | Description |
| ---- | ----------- |
| `StopLoss` | Distance in ticks for the protective stop. |
| `TakeProfit` | Distance in ticks for the profit target. |
| `RiskPercent` | Percent of account equity risked on each trade. |
| `CandleType` | Candle type and timeframe for analysis (default: 4-hour candles). |

## Notes

The strategy is long-only and designed for educational purposes. Adjust parameters and risk settings before using it in live trading.
