# Double ZigZag Alignment 策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MQL5 专家顾问「Double ZigZag」的 StockSharp 版本。通过两个不同窗口的摆动检测器来复现双 ZigZag 的确认逻辑。
只有当两个检测器在最近三个关键点上完全一致且最新波动相对更强时才会触发交易。

## 核心思想

- 快速检测器使用滑动最高值/最低值窗口来逼近原始 ZigZag(13, 5, 3) 设置。
- 慢速检测器使用更长的窗口（默认放大 8 倍）过滤噪声并确认主要拐点。
- 当两个检测器在同一根 K 线同时翻转方向时，会记录一个“对齐”枢轴以及自上次对齐以来出现的快速
  摆动次数，这些计数对应原程序中的 `up` 和 `dw`。

## 多头条件

1. 最近一次对齐是高点，再上一对齐是低点，再往前也是高点。
2. 自最近对齐以来的快速摆动数量大于前一个区段的数量乘以 `StrengthMultiplier`（默认 2.1），即
   `up > dw * k`。
3. 最新高点突破中间低点的力度要强于更早的高点：`(previousHigh - swingLow) * BreakoutMultiplier <
   (newestHigh - swingLow)`。
4. 满足条件后买入 `Volume` 加上现有空头仓位的数量，使净头寸变为多头。

## 空头条件

1. 最近一次对齐是低点，再上一对齐是高点，再往前也是低点。
2. 最近区段的数量小于前一段乘以 `StrengthMultiplier`，即 `up * k < dw`。
3. 当前低点向下突破中间高点的幅度大于更早的低点，使用同样的 `BreakoutMultiplier`。
4. 卖出足够的数量以平掉多头并建立净空头仓位。

## 仓位管理

- 信号互斥，新信号会自动反向持仓。
- 不设置止损或止盈，平仓完全依赖反向信号。
- 分析使用 `CandleType` 指定的 K 线类型（默认 1 分钟）。

## 默认参数

- `FastLength` = 13
- `SlowLength` = 104
- `StrengthMultiplier` = 2.1
- `BreakoutMultiplier` = 2.1
- `CandleType` = `TimeSpan.FromMinutes(1)` 时间框架

## 标签

- **类别**：趋势/形态识别
- **方向**：双向
- **指标**：ZigZag（近似实现）、Highest/Lowest
- **止损**：无
- **时间框架**：默认日内
- **复杂度**：中等（需要同步跟踪摆动）
- **季节性**：无
- **神经网络**：无
- **背离**：无
- **风险等级**：中等（无保护性止损）
