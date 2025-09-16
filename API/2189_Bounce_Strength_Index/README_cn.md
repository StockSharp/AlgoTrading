# Bounce Strength Index 策略

该策略实现了简化的 Bounce Strength Index（BSI）指标。该指标计算收盘价在最近区间中的位置，并通过两次 **SimpleMovingAverage** 平滑来突出动量变化。

## 逻辑
- 使用 **Highest** 和 **Lowest** 指标计算最近区间的最高价和最低价。
- 计算收盘价在该区间中的位置，并进行两次平滑。
- 当指标向上转折时，平掉空头并开多头。
- 当指标向下转折时，平掉多头并开空头。

## 参数
- `CandleType` – 使用的K线类型。
- `RangePeriod` – 计算区间的长度。
- `Slowing` – 快速平滑长度。
- `AvgPeriod` – 慢速平滑长度。

## 指标
- BounceStrengthIndex（自定义）
- Highest
- Lowest
- SimpleMovingAverage
