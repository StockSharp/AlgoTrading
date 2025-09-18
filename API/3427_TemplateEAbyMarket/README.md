# TemplateEAbyMarket Strategy

## Overview
TemplateEAbyMarket is a direct StockSharp port of the original MetaTrader 4 expert advisor *TemplateEAbyMarket.mq4*. The strategy uses the Moving Average Convergence Divergence (MACD) indicator to detect momentum shifts. When the MACD main line crosses the signal line while both components are in the same positive or negative zone, the strategy opens a market position in the direction of the crossover. Exits are managed exclusively through protective orders (take profit and stop loss) configured via the built-in `StartProtection` helper.

The StockSharp version keeps the behaviour of the MQL program: it only opens new positions without trying to automatically close the opposite side. Once a position is filled, the trade is left to be managed by protective levels or manual intervention.

## Trading Logic
1. Subscribe to the user-selected candle type (default: 15-minute time frame).
2. Calculate MACD (12/26/9 by default) on every finished candle.
3. Track the relative position of the MACD main and signal lines to detect a crossover event:
   - **Bullish setup:** previous candle had the main line below the signal line, the current candle closes with the main line above the signal line, and both lines are above zero. A market buy order with `OrderVolume` is submitted if the current exposure is below `MaxOrders * OrderVolume`.
   - **Bearish setup:** previous candle had the main line above the signal line, the current candle closes with the main line below the signal line, and both lines are below zero. A market sell order with `OrderVolume` is submitted subject to the same exposure cap.
4. Protective `takeProfit` and `stopLoss` levels are activated once at start-up. The strategy does not close opposite positions automatically; risk is controlled by the protection module or by the user.

## Parameters
| Name | Description |
|------|-------------|
| `MacdFastPeriod` | Fast EMA length for the MACD calculation. |
| `MacdSlowPeriod` | Slow EMA length for the MACD calculation. |
| `MacdSignalPeriod` | Signal EMA length for the MACD calculation. |
| `CandleType` | Candle type (time frame) that feeds the indicator. |
| `OrderVolume` | Volume submitted with each market order. |
| `MaxOrders` | Maximum number of concurrent orders, expressed as multiples of `OrderVolume`. The strategy checks `abs(Position) < MaxOrders * OrderVolume` before sending a new order. |
| `TakeProfitPoints` | Take-profit distance in price points. Value `0` disables the take profit. |
| `StopLossPoints` | Stop-loss distance in price points. Value `0` disables the stop loss. |

## Notes
- Slippage and magic number settings from the MQL version are intentionally omitted because they are handled differently in StockSharp.
- Ensure the connector provides proper price step metadata; `StartProtection` interprets distances in instrument price points.
- The template is intentionally minimalistic and does not manage partial fills or pyramid entries beyond the `MaxOrders` limit.
