# EM VOL 策略

该策略基于枢轴水平的突破进行交易。
使用前一根K线的最高价和最低价加减ATR形成入场触发。
当ADX指标显示低波动时才允许开仓。

## 逻辑

1. 计算上一根K线的高点和低点，并加减ATR得到阻力位和支撑位。
2. 当ADX低于阈值且价格收于阻力位之上时，做多。
3. 当价格收于支撑位之下时，做空。
4. 开仓后同时设置止损和止盈。
5. 达到指定利润后，启用跟踪止损。

## 参数

- `TakeProfit` — 止盈距离（价格步长）。
- `StopLoss` — 止损距离（价格步长）。
- `AtrPeriod` — ATR周期。
- `AdxPeriod` — ADX周期。
- `AdxThreshold` — 允许交易的最大ADX值。
- `TrailStart` — 启动跟踪止损所需的利润。
- `TrailStep` — 跟踪止损距离。
- `CandleType` — 计算所用的时间框。

## 使用的指标

- Average True Range
- Average Directional Index
