# Renko Level EA 策略
[English](README.md) | [Русский](README_ru.md)

## 概述
- 将 MetaTrader 专家顾问 **Renko Level EA.mq5** 转换到 StockSharp 框架。
- 通过 `BrickSize` 参数计算虚拟的 Renko 上下水平线，并跟踪它们的移动。
- 使用 `CandleType` 定义的收盘价（默认 1 分钟 K 线）驱动逻辑，只在 K 线完成后做出决策。
- 没有固定止损或止盈，所有离场都依靠反向信号完成。

## 交易逻辑
1. 第一根完成的 K 线会把收盘价四舍五入到 Renko 网格，初始化上下两个水平。
2. 随后的每根 K 线：
   - 收盘价位于区间内时，水平保持不变。
   - 收盘价突破上轨，Renko 方块向上平移到下一个格子。
   - 收盘价跌破下轨，Renko 方块向下移动。
3. 只要上轨发生变化，就视为方向切换：
   - 上轨上升 → 多头信号（若 `ReverseSignals` 为 `true` 则反向）。
   - 上轨下降 → 空头信号。
4. `ReverseSignals` 与 `AllowIncrease` 分别对应原 EA 的反向和加仓开关，可灵活复现不同行为。

## 仓位管理
- 做多前会先平掉所有空头仓位，做空前会先平掉多头仓位。
- 当 `AllowIncrease = false` 时，只有在当前方向没有持仓时才会再次下单。
- 当 `AllowIncrease = true` 时，即使已经持有同向仓位，也会按 `OrderVolume` 继续加仓。
- 不设止损/止盈，仓位在出现反向信号时被平仓或反转。
- 调用 `StartProtection()` 以启用框架内置的安全保护。

## 参数
| 名称 | 说明 | 默认值 | 可优化 |
| --- | --- | --- | --- |
| `BrickSize` | 以 `Security.PriceStep` 为单位的 Renko 方块高度，决定触发信号所需的价格位移。 | `30` | 是（10 → 100，步长 10） |
| `OrderVolume` | 每次市价单的下单数量。 | `1` | 否 |
| `ReverseSignals` | 交换多空方向，对应原始 EA 的 *Reverse* 选项。 | `false` | 否 |
| `AllowIncrease` | 允许在已有仓位的情况下继续加仓，对应 EA 的 *Increase* 参数。 | `false` | 否 |
| `CandleType` | 用于计算的 K 线类型，默认 1 分钟时间框，可替换为任意受支持的序列。 | `TimeFrameCandleMessage(1m)` | 否 |

## 实践提示
- `BrickSize` 会自动乘以交易品种的最小报价步长，因此可用于外汇、期货和加密货币等不同市场。
- 决策只依赖收盘价，盘中波动只有在形成最终收盘时才被纳入计算。
- 同时启用 `ReverseSignals` 与 `AllowIncrease` 可以探索反趋势或金字塔加仓等变体。
- 适合想要跟随 Renko 突破、不依赖额外指标过滤的策略研究。

## 分类
- **策略类型**：趋势跟随（Renko 突破）。
- **方向**：多/空。
- **复杂度**：中等（自定义水平跟踪，参数少）。
- **止损**：无，依靠反向信号退出。
- **时间框架**：由 `CandleType` 决定。
- **指标**：自建 Renko 水平。
