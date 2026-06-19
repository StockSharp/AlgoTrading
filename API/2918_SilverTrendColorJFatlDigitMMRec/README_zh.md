# SilverTrend ColorJFatl Digit MMRec 策略

## 概述

本策略是 MetaTrader 顾问 `Exp_SilverTrend_ColorJFatl_Digit_MMRec` 的 StockSharp 版本。它保留了原始系统的双模块设计：

- **SilverTrend 模块**：根据 SilverTrend 指标的蜡烛颜色，识别价格突破自适应通道的时刻。
- **ColorJFatl 模块**：利用官方系数表计算 FATL（Fast Adaptive Trend Line），并使用 EMA 平滑器模拟 MetaTrader 中的 Jurik 平滑效果。

两个模块分别维护各自的虚拟仓位，可独立开多或开空，也可以在出现反向信号时平掉对侧仓位，并应用独立的止损/止盈距离。策略的最终净头寸等于两个模块虚拟仓位的代数和。

## 默认设置

- 时间框架：默认情况下两个模块都订阅 6 小时蜡烛，可通过参数修改。
- 交易数量：每个模块都有独立的下单量参数（默认 `1`）。
- 交易品种：由用户在 StockSharp 中选择的证券。

## 指标与信号逻辑

### SilverTrend 模块

1. 使用最近 `SSP` 根蜡烛构建高低价通道。
2. 通过 `(33 - Risk) / 100` 系数收缩通道边界（与原始 MQL 实现一致）。
3. 将蜡烛按趋势着色：`0/1` 表示多头，`3/4` 表示空头，`2` 表示中性。
4. 触发条件：
   - **做多**：位于参数 `Signal Bar` 指定位置的蜡烛变为多头颜色，而更早一根蜡烛不是多头。
   - **做空**：同一位置的蜡烛变为空头颜色，而更早一根蜡烛不是空头。
5. 止损和止盈以点数设置，并根据交易标的的 `PriceStep` 转换为价格距离。

### ColorJFatl 模块

1. 根据选择的 `Applied Price` 类型，通过 FATL 系数表计算基础序列。
2. 使用长度为 `JMA Length` 的 EMA 平滑该序列，`JMA Phase` 参数保留用于描述和兼容性（在当前实现中不直接影响计算）。
3. 根据平滑后的数值斜率设置颜色：上升为 `2`，下降为 `0`，持平时保持前一颜色。
4. 触发条件：
   - **做多**：颜色从 `0/1` 切换到 `2`。
   - **做空**：颜色从 `1/2` 切换到 `0`。
5. 可选择在开仓前自动平掉模块内的反向仓位。

## 风险管理

- 两个模块分别记录各自的入场价以及止损/止盈距离。
- 当任一模块触发止损或止盈时，仅关闭该模块的虚拟仓位，另一模块保持不变。
- 当两个模块同时看多或看空时，它们的下单量会叠加。

## 参数说明

| 分组 | 参数 | 说明 |
| --- | --- | --- |
| SilverTrend | `Silver Candle Type` | SilverTrend 模块使用的蜡烛类型。 |
| SilverTrend | `SSP` | 构建高低价范围的长度。 |
| SilverTrend | `Risk` | 通道收缩系数。 |
| SilverTrend | `Signal Bar` | 读取信号时使用的蜡烛偏移。 |
| SilverTrend | `Allow Silver Long/Short` | 是否允许多/空方向的开仓。 |
| SilverTrend | `Close Silver Long/Short` | 是否允许自动平掉反向仓位。 |
| SilverTrend | `Silver Volume` | SilverTrend 模块下单量。 |
| SilverTrend | `Silver SL/TP` | 止损和止盈的点数距离。 |
| ColorJFatl | `Color Candle Type` | ColorJFatl 模块使用的蜡烛类型。 |
| ColorJFatl | `JMA Length` | FATL 平滑的长度。 |
| ColorJFatl | `JMA Phase` | 保留的 Jurik 相位参数。 |
| ColorJFatl | `Applied Price` | FATL 计算所使用的价格类型。 |
| ColorJFatl | `Digits` | FATL 数值的保留小数位数。 |
| ColorJFatl | `Color Signal Bar` | 读取 FATL 信号的蜡烛偏移。 |
| ColorJFatl | `Allow/Close` | 多/空开仓及是否自动平仓的开关。 |
| ColorJFatl | `Color Volume` | ColorJFatl 模块下单量。 |
| ColorJFatl | `Color SL/TP` | 模块的止损、止盈点数。 |

## 使用建议

1. 确认证券的 `PriceStep` 已正确设置，否则点数止损/止盈无法换算为价格。
2. 可以单独开启或关闭任一模块，逐步验证它们的表现，再组合使用。
3. 模块可以在相反方向持仓，策略最终头寸为两个模块仓位的和，因此可能出现多空对冲的情况。
4. 针对目标市场优化 `SSP`、`Risk`、`JMA Length` 以及价格类型，可显著改善信号质量。
