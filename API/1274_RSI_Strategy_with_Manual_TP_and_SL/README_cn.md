# RSI 策略（手动止盈止损）

当 RSI 从下向上突破超卖线且收盘价高于最近 50 根 K 线最高价的 70% 时做多；当 RSI 从上向下跌破超买线且收盘价低于最近 50 根 K 线最低价的 130% 时做空。仓位通过百分比止盈和止损进行保护。

## 参数

- **Candle Type** – K线周期。
- **RSI Length** – RSI 周期。
- **Oversold Level** – 超卖阈值。
- **Overbought Level** – 超买阈值。
- **Lookback** – 最高/最低计算周期。
- **Take Profit %** – 止盈百分比。
- **Stop Loss %** – 止损百分比。
