# BTC Chop Reversal Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades short-term reversals on BTC when price tests ATR bands and momentum shifts, combining EMA, ATR, RSI, MACD histogram, and a volume spike filter.

## Details

- **Entry Criteria**:
  - **Long**: `Low < EMA - ATR*Mult` && `RSI < Oversold` && `MACD hist rising` && `Close > Open` && no sell volume spike.
  - **Short**: `High > EMA + ATR*Mult` && `RSI > Overbought` && `MACD hist falling` && `Close < Open`.
- **Long/Short**: Both.
- **Exit Criteria**:
  - Positions protected by take-profit and stop-loss.
- **Stops**: Take profit 0.75%, Stop loss 0.4%.
- **Default Values**:
  - `EMA Period` = 23.
  - `ATR Length` = 55.
  - `ATR Multiplier` = 4.4.
  - `RSI Length` = 9.
  - `RSI Overbought` = 68.
  - `RSI Oversold` = 28.
  - `MACD Fast` = 14.
  - `MACD Slow` = 44.
  - `MACD Signal` = 3.
  - `Volume MA Length` = 16.
  - `Sell Spike Multiplier` = 1.5.
  - `Take Profit (%)` = 0.75.
  - `Stop Loss (%)` = 0.4.
- **Filters**:
  - Category: Reversal.
  - Direction: Both.
  - Indicators: EMA, ATR, RSI, MACD, Volume.
  - Stops: Yes.
  - Complexity: Medium.
  - Timeframe: Short-term.
  - Seasonality: No.
  - Neural networks: No.
  - Divergence: No.
  - Risk level: Medium.
