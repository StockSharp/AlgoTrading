# Zonal Trading 策略

该策略结合了 Awesome Oscillator (AO) 和 Accelerator Oscillator (AC)，用于捕捉市场动量的变化。

## 逻辑
- 当 AO 和 AC 同时上升并且至少有一个从前一根柱子向上转折且两者都为正时买入。
- 当 AO 和 AC 同时下降并且至少有一个从前一根柱子向下转折且两者都为负时卖出。
- 当 AO 和 AC 同时转向下时平掉多单。
- 当 AO 和 AC 同时转向上时平掉空单。

## 参数
- **Candle Type** – 用于计算的K线类型。
- **Take Profit** – 以价格单位表示的固定止盈。

该策略一次只持有一个仓位，并使用市价单交易。
