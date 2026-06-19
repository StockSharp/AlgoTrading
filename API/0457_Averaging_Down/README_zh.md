# Averaging Down Strategy

当价格突破以EMA为中心的ATR通道时开仓。若价格逆向运行，策略按照阶梯百分比偏移进行加仓（DCA）。当价格回到平均建仓价并达到设定的利润百分比时平仓。

## 参数
- Candle Type – 使用的K线类型。
- EMA Length – EMA周期。
- ATR Length – ATR周期。
- ATR Mult – ATR通道倍数。
- TP % – 从平均价计算的止盈百分比。
- Base Deviation % – 第一次加仓的初始偏移百分比。
- Step Scale – 每次加仓偏移的倍增系数。
- DCA Size Multiplier – 每次加仓的量倍增系数。
- Max DCA Levels – 最大加仓次数。
- Initial Volume – 初始下单量。
