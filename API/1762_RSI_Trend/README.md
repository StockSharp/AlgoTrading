# RSI Trend Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The **RSI Trend Strategy** uses the Relative Strength Index (RSI) to detect trend reversals and manages positions with an ATR-based trailing stop. The system opens a long position when the RSI crosses above an overbought threshold and enters a short position when the RSI falls below an oversold threshold. Risk is controlled using a trailing stop derived from the Average True Range (ATR), allowing the stop level to adapt to current volatility.

This implementation is designed for educational purposes and demonstrates how to build a high-level StockSharp strategy using indicator bindings. The strategy trades on completed candles only and does not reference previous indicator values directly, aligning with StockSharp best practices.

## Details

- **Entry Criteria**:
  - **Long**: `RSI(t) > BuyLevel` and `RSI(t-1) <= BuyLevel`.
  - **Short**: `RSI(t) < SellLevel` and `RSI(t-1) >= SellLevel`.
- **Long/Short**: Both directions.
- **Exit Criteria**:
  - Trailing stop based on ATR multiple.
- **Stops**: Yes, dynamic trailing stop.
- **Default Values**:
  - `RSI Period` = 14.
  - `BuyLevel` = 73.
  - `SellLevel` = 27.
  - `ATR Period` = 100.
  - `ATR Multiple` = 3.
- **Filters**:
  - Category: Trend following.
  - Direction: Both.
  - Indicators: RSI, ATR.
  - Stops: Yes.
  - Complexity: Medium.
  - Timeframe: Any (default 1 minute candles).
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Moderate.

