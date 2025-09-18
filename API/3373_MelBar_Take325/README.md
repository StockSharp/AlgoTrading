# MelBar Take325 Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

The MelBar Take325 strategy is a direct conversion of the Expert Advisor Studio system "MelBar™Take325%™ 5.5Y NZD-USD". It trades both directions on NZD/USD using a combination of tick volume breakouts, a swing filter based on a 12-period simple moving average, and a 14-period RSI exit filter. The StockSharp port keeps the original risk parameters of a 16-pip stop loss and a 45-pip take profit, expressed in pip distances from the entry price.

The strategy starts by waiting for an increase in tick volume, defined as a breakout above the configured volume threshold. When volume expands, it checks whether the simple moving average formed a local turning point two bars earlier. A local maximum in the SMA opens a long trade, while a local minimum opens a short trade. Only one direction can be taken at a time, and conflicting signals are ignored to avoid flip-flopping on the same bar.

Open positions are actively managed. Stop-loss and take-profit levels are enforced every time a candle closes, making the behaviour similar to the MetaTrader version. Additionally, the 14-period RSI is used to force exits: long trades close when RSI crosses down through the configured level (default 80), and short trades close when RSI crosses up through the symmetric level (default 20). The high/low of the processed candle is compared with the entry price to trigger stop-loss and take-profit exits.

## Details

- **Entry Criteria**:
  - **Volume filter**: tick volume two bars ago must be below the threshold while the previous bar exceeds it.
  - **Long**: SMA (length 12) has a local peak two bars ago (`SMA[t-3] < SMA[t-2]` and `SMA[t-2] > SMA[t-1]`).
  - **Short**: SMA has a local trough (`SMA[t-3] > SMA[t-2]` and `SMA[t-2] < SMA[t-1]`).
- **Exit Criteria**:
  - **Stop-loss**: 16 pips from entry, evaluated on candle closes.
  - **Take-profit**: 45 pips from entry, evaluated on candle closes.
  - **Long RSI exit**: RSI crosses downward through 80 (`RSI[t-3] > 80` and `RSI[t-2] < 80`).
  - **Short RSI exit**: RSI crosses upward through 20 (`RSI[t-3] < 20` and `RSI[t-2] > 20`).
- **Default Parameters**:
  - Entry volume = 0.1 lots.
  - Volume threshold = 1000 tick volume units.
  - SMA period = 12.
  - RSI period = 14.
  - RSI level = 80 (short exit uses 100 - level).
  - Candle timeframe = 30 minutes.
- **Market**: Designed for NZD/USD but can be applied to other FX pairs.
- **Style**: Momentum breakout with mean-reversion exits.
- **Stops**: Fixed stop-loss and take-profit; no trailing stop in the original code.
- **Complexity**: Moderate; combines multiple filters but no position scaling.
- **Risk**: Medium, as the stop is tighter than the take-profit but both are fixed distances.
