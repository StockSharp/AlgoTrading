# RSI 背离策略 - AliferCrypto

该策略基于 RSI 背离，并可选 RSI 区域和趋势过滤器。止损和止盈可根据摆动或 ATR 计算，可动态更新或在入场时锁定。

## 逻辑
- **入场**
  - 看涨背离：价格创更低低点而 RSI 创更高低点。
  - 看跌背离：价格创更高高点而 RSI 创更低高点。
  - 可选 RSI 区域过滤需先进入超卖/超买状态。
  - 可选趋势过滤使用移动平均方向。
- **出场**
  - 止损/止盈基于最近摆动或 ATR。
  - 水平可在入场锁定或每根 K 线重新计算。

## 指标
- Relative Strength Index
- Moving Average
- Average True Range
- Highest/Lowest
