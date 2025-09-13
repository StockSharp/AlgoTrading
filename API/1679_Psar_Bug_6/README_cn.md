# Psar Bug 6 Strategy

从 MQL4 脚本 “psar_bug_6” 转换而来。

## 逻辑
- 使用可配置加速步长和最大值的抛物线 SAR 指标。
- 当价格收盘突破 SAR 上方且之前在下方时做多。
- 当价格收盘跌破 SAR 下方且之前在上方时做空。
- `Reverse` 参数可以反转买卖信号。
- `SarClose` 选项在 SAR 翻转到另一侧时平掉现有仓位。
- 固定的止盈和止损距离，以价格单位表示，可选择启用跟踪止损。

## 参数
- `SarStep` – 加速因子步长。
- `SarMax` – 最大加速因子。
- `StopLoss` – 初始止损距离。
- `TakeProfit` – 止盈距离。
- `Trailing` – 是否启用跟踪止损。
- `TrailStop` – 启用跟踪时的止损距离。
- `SarClose` – SAR 反转时是否平仓。
- `Reverse` – 是否反转信号。
- `CandleType` – 计算所用的蜡烛类型。

## 说明
策略使用高级 API，通过蜡烛订阅和指标绑定运行，并通过可选的跟踪止损启动保护，出场使用市价单。
