# Momentum Candle Sign Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy trades based on the crossover between momentum values calculated from candle open and close prices. When the momentum of the open price drops below the momentum of the close price, it signals rising bullish pressure and the strategy enters a long position. The opposite crossover indicates bearish pressure and triggers a short position.

By default the strategy operates on 12-hour candles with a momentum period of 12.

## Details

- **Entry Criteria**:
  - **Long**: Open momentum crosses below close momentum.
  - **Short**: Open momentum crosses above close momentum.
- **Long/Short**: Both directions.
- **Exit Criteria**: Opposite crossover.
- **Stops**: None.
- **Filters**: None.
