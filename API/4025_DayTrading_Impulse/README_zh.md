# Day Trading Impulse 策略

## 概述

**DayTrading Strategy** 是对 2005 年 NazFunds 公司发布的 MetaTrader 4 智能交易程序「DayTrading」的 C# 复刻版本。原策略建议在 5 分钟外汇图上运行，通过多重趋势与动量指标的组合，在短时间窗口内捕捉方向性走势，并搭配固定止盈与可选的移动止损。本次在 StockSharp 上的实现完整保留了 MQL 逻辑，同时将关键阈值暴露为可优化参数，方便针对不同品种进行调优。

## 指标体系

策略会在选定的 K 线订阅上计算以下四个指标：

- **Parabolic SAR** (`ParabolicSar`)：可配置起始加速度、步长及最大值。指标位置必须翻转到价格的另一侧才能解锁新的入场。
- **MACD (12, 26, 9)** (`MovingAverageConvergenceDivergenceSignal`)：多头要求 MACD 主线低于信号线，空头则相反，对应 MT4 中对主线与信号线的比较。
- **随机指标 Stochastic (5, 3, 3)** (`StochasticOscillator`)：%K 低于 35 允许做多，%K 高于 60 允许做空，确保行情从超卖/超买区域回落。
- **动量指标 Momentum (14)** (`Momentum`)：低于 100 解锁多头，高于 100 解锁空头，完全复现原脚本的判断方式。

所有指标都通过高层的 `BindEx` 管线连接，无需手动维护历史缓冲或索引。

## 交易规则

### 入场条件

在最后一根完结 K 线上，若满足以下条件则开 **多仓**：

1. Parabolic SAR 点位于当前买价（ask）或以下，且上一根 SAR 点高于当前点（出现新的向上翻转）。
2. Momentum < 100。
3. MACD 主线 < 信号线。
4. Stochastic %K < 35。

开 **空仓** 的条件互为镜像：

1. Parabolic SAR 点位于当前卖价（bid）或以上，且上一根 SAR 点低于当前点（向下翻转）。
2. Momentum > 100。
3. MACD 主线 > 信号线。
4. Stochastic %K > 60。

策略始终只持有一笔仓位。当出现反向信号时，会先平掉当前仓位，本根 K 线内不会立即再次开仓——这一行为与原始 EA 在 `OrdersTotal` 循环中的处理一致。

### 离场逻辑

- **止损 / 止盈：** 可选的固定点差会转换为绝对价格，并在每根 K 线上检测。一旦触发即平仓。
- **移动止损：** 当价格按照设定点数运行后，自动启动跟踪。多头将止损上移到收盘价下方，空头则下移到收盘价上方；止损不会后退，可逐步锁定利润。
- **反向信号：** 一旦出现满足条件的反向信号，立即平掉持仓，然后等待下一次机会。

策略不包含加仓、网格或对冲等附加逻辑，保持与原 EA 相同的简洁风格。

## 参数说明

| 参数 | 默认值 | 说明 |
| --- | --- | --- |
| `LotSize` | 1 | 每笔市价单的手数。启动时会同步到 `Strategy.Volume`。 |
| `TrailingStopPoints` | 15 | 移动止损的点数，0 表示禁用。 |
| `TakeProfitPoints` | 20 | 固定止盈点数，0 表示无固定目标。 |
| `StopLossPoints` | 0 | 固定止损点数，0 复现原策略的「无止损」设置。 |
| `SlippagePoints` | 3 | 允许的滑点（为兼容 MT4 输入而保留，代码中不会强制使用）。 |
| `CandleType` | 5 分钟 | 指标所用的蜡烛类型。保持为 M5 可与原版效果一致。 |
| `MacdFastPeriod` | 12 | MACD 快速 EMA 长度。 |
| `MacdSlowPeriod` | 26 | MACD 慢速 EMA 长度。 |
| `MacdSignalPeriod` | 9 | MACD 信号 EMA 长度。 |
| `StochasticLength` | 5 | 随机指标 %K 的基础周期。 |
| `StochasticSignal` | 3 | %D 平滑周期。 |
| `StochasticSlow` | 3 | %K 终端平滑周期。 |
| `MomentumPeriod` | 14 | Momentum 的回溯周期。 |
| `SarAcceleration` | 0.02 | Parabolic SAR 的起始加速度。 |
| `SarStep` | 0.02 | Parabolic SAR 的加速度增量。 |
| `SarMaximum` | 0.2 | Parabolic SAR 的最大加速度。 |

所有数值参数都已标记 `SetCanOptimize(true)`，可直接在 StockSharp 优化器中做批量搜索。

## 实现细节

- 当 Level1 行情提供最优买卖价时使用其作为判断基础；若缺失，则回退到蜡烛收盘价，保证历史回测的稳定性。
- 点值换算优先采用 `Security.Step` 或 `PriceStep`，若没有配置则退化为 0.0001，与常见外汇品种的最小点差一致。
- 策略始终保持单向持仓，不会同时持有多空，也不会分批加仓。
- 源码中的注释全部为英文以符合仓库规范，而本 README 提供了更详细的中文说明。

## 使用建议

1. 指定目标货币对，保持 5 分钟周期，启动策略即可。所有指标会自动完成热身。
2. 在真实账户中建议启用非零止损。虽然原作者主张无止损，但仅依靠移动止损可能不足以防范极端行情。
3. 可以将该策略加入 `BasketStrategy`，在组合层面对资金进行统一调度，同时利用参数化能力进行优化或蒙特卡洛测试。

文件夹中还提供了英文与俄文版本的文档，便于团队协作参考。
