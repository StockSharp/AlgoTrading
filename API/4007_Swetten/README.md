# Swetten Strategy

## Overview
Swetten is a neural-network driven breakout strategy that was originally distributed for MetaTrader 4. It evaluates the spread between a long-term 233-period simple moving average and ten faster moving averages calculated on one-minute candles. The spreads are fed into a radial basis network that produces a bullish or bearish activation level. When the activation is positive the strategy enters long, when it is negative it enters short.

## Market & Timeframe
- Designed for major FX pairs (the original code targeted EURUSD).
- Analysis uses one-minute candles and decisions are made only on completed candles.
- Signals are evaluated every two hours at the top of the hour (00:00, 02:00, …, 22:00 exchange time). No trades are opened on Saturdays or Sundays.

## Indicators and Features
- Simple moving averages with periods: 233 (baseline), 144, 89, 55, 34, 21, 13, 8, 5, 3, 2.
- Neural network inputs are the differences between the 233-period average and each faster average.
- Before passing to the network the inputs are clamped to trained ranges, normalized, and scaled with the same coefficients used in the original DLL.
- The radial basis network is replicated exactly from the exported `EURUSDn` function, consisting of 38 Gaussian features whose outputs are averaged to obtain the final activation.

## Trading Rules
1. Wait for the close of a one-minute candle that ends on an even hour and falls on a weekday.
2. Compute the neural network activation from the moving-average spreads.
3. If activation &gt; 0 and the current position is not long, send a market buy for `TradeVolume + abs(current position)` lots.
4. If activation &lt; 0 and the current position is not short, send a market sell for `TradeVolume + abs(current position)` lots.
5. Positions are protected by:
   - A fixed take profit defined in price steps (`TakeProfitPoints`).
   - A fixed stop loss defined in price steps (`StopLossPoints`).
   - When either level is touched using candle high/low extremes, the position is closed by a market order.

## Parameters
| Name | Description | Default |
| --- | --- | --- |
| `CandleType` | Candle series used for calculations. | 1-minute time frame |
| `TradeVolume` | Base order volume in lots. | 0.1 |
| `SlowPeriod` | Period of the baseline simple moving average. | 233 |
| `TakeProfitPoints` | Profit target distance in price steps. | 150 |
| `StopLossPoints` | Stop-loss distance in price steps. | 40 |

## Conversion Notes
- The DLL-based neural network from MetaTrader was fully ported to C# by translating the exported function to managed code.
- Protective exits mimic the original `OrderClose` conditions by checking candle highs and lows against price step thresholds.
- Entry management keeps track of the latest fill price via `OnNewMyTrade` to align exits with actual fills.
- All comments were rewritten in English and the code uses high-level StockSharp APIs (`SubscribeCandles`, `Bind`) as required by the conversion guidelines.
