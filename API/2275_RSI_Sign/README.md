# RSI Sign Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy converts the original **iRSISign** expert advisor from MQL5 into the StockSharp high level API. It combines the Relative Strength Index (RSI) with the Average True Range (ATR) to generate entry and exit signals.

The system listens to finished candles of a user defined timeframe. When the RSI crosses above the lower threshold it signals a potential bullish reversal and opens a long position or closes an existing short. Conversely, when RSI falls below the upper threshold it enters a short position or closes an active long. ATR is calculated but used only for additional context, mirroring the original indicator that displayed signal arrows offset by ATR.

## Details

- **Entry Criteria**:
  - **Long**: Previous RSI value was below `DownLevel` and current RSI crosses above it.
  - **Short**: Previous RSI value was above `UpLevel` and current RSI crosses below it.
- **Long/Short**: Both directions are allowed and can be independently enabled.
- **Exit Criteria**:
  - Opposite signal closes the current position if the corresponding close flag is enabled.
- **Stops**: Not implemented. Risk management can be added externally if needed.
- **Default Values**:
  - `RsiPeriod` = 14
  - `AtrPeriod` = 14
  - `UpLevel` = 70
  - `DownLevel` = 30
  - `CandleType` = 1 hour candles
- **Filters**:
  - Category: Momentum
  - Direction: Both
  - Indicators: RSI, ATR
  - Stops: No
  - Complexity: Basic
  - Timeframe: Flexible
  - Seasonality: No
  - Neural networks: No
  - Divergence: No
  - Risk level: Medium

## Parameters

| Name | Description |
|------|-------------|
| `RsiPeriod` | RSI length. |
| `AtrPeriod` | ATR length. |
| `UpLevel` | RSI upper threshold generating sell signals. |
| `DownLevel` | RSI lower threshold generating buy signals. |
| `CandleType` | Candle timeframe used for calculations. |
| `BuyOpen` | Enable opening of long positions. |
| `SellOpen` | Enable opening of short positions. |
| `BuyClose` | Allow closing of existing longs on opposite signal. |
| `SellClose` | Allow closing of existing shorts on opposite signal. |

The strategy is intended as an educational example demonstrating how to translate simple MQL5 logic into StockSharp's high level strategy framework.
