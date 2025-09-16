# BuySell Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy emulates the **BuySell** MetaTrader expert. It combines a moving average with the Average True Range (ATR) to detect trend reversals.
When the moving average turns upward, the system considers the market bullish; when it turns downward, it considers the market bearish.
A trade is opened only if the previous bar was in the opposite state, confirming a reversal. Optional stop-loss and take-profit levels are expressed in price points.

## Details

- **Entry Logic**
  - **Long**: moving average changes from falling to rising and the previous bar was bearish.
  - **Short**: moving average changes from rising to falling and the previous bar was bullish.
- **Exit Logic**
  - **Long**: moving average turns down or stop-loss / take-profit triggers.
  - **Short**: moving average turns up or stop-loss / take-profit triggers.
- **Indicators**: Simple Moving Average (SMA) and ATR.
- **Stops**: Both stop-loss and take-profit in points.
- **Permissions**: separate flags allow or forbid opening/closing long and short positions.
- **Default Timeframe**: 4-hour candles.

## Parameters

| Name | Default | Description |
| ---- | ------- | ----------- |
| `MaPeriod` | 14 | Moving average period. |
| `AtrPeriod` | 60 | ATR period. |
| `StopLoss` | 1000 | Stop-loss in price points. |
| `TakeProfit` | 2000 | Take-profit in price points. |
| `AllowLongEntry` | true | Permission to open long positions. |
| `AllowShortEntry` | true | Permission to open short positions. |
| `AllowLongExit` | true | Permission to close long positions. |
| `AllowShortExit` | true | Permission to close short positions. |
| `CandleType` | H4 | Timeframe used for calculations. |

## Usage

1. Add the strategy to your StockSharp solution.
2. Configure the parameters as needed.
3. Run the strategy in live or backtest mode. Trades are executed using `BuyMarket` and `SellMarket` orders.

The approach is suitable for markets where trend reversals are accompanied by volatility changes captured by ATR.
