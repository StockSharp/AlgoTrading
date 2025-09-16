# RobotPower M5 策略

该策略在5分钟K线图上结合 Bulls Power 和 Bears Power 指标。
当两者之和穿越零轴时开仓，并通过固定目标和跟踪止损退出。

## 工作原理
- **指标**：Bulls Power 与 Bears Power，周期均为 `BullBearPeriod`。
- **时间周期**：默认使用5分钟K线 (`CandleType`)。

### 入场规则
- **做多**：当 `BullsPower + BearsPower > 0` 且没有持仓时市价买入。
- **做空**：当 `BullsPower + BearsPower < 0` 且没有持仓时市价卖出。

### 出场规则
- **止盈**：价格朝持仓方向移动 `TakeProfit` 单位时平仓。
- **止损**：价格逆势移动 `StopLoss` 单位时平仓。
- **跟踪止损**：开仓后当价格超过 `TrailingStep` 的两倍时，止损向盈利方向移动 `TrailingStep`。

### 参数
- `BullBearPeriod` – Bulls Power 和 Bears Power 的计算周期。
- `TrailingStep` – 跟踪止损移动的步长。
- `TakeProfit` – 开仓价到止盈价的距离。
- `StopLoss` – 开仓价到止损价的距离。
- `CandleType` – 用于计算信号的K线时间周期。

### 仓位大小
使用策略的 `Volume` 属性作为下单数量。

## 说明
本策略示例展示了如何将 MQL 策略转换为 StockSharp API，仅用于学习目的。
