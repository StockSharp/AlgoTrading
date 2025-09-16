# Exp Super Trend 策略
[English](README.md) | [Русский](README_ru.md)

该策略由 MQL 脚本 **Exp_Super_Trend.mq5**（ID 14269）转换而来。它跟随 SuperTrend 指标的方向，在趋势反转时立即反向开仓。实现基于 StockSharp 的高级 API，并使用内置的 SuperTrend 指标。

该指标依据 ATR 计算动态支撑或阻力线。价格高于该线时视为多头趋势，低于该线时视为空头趋势。策略在多头趋势期间建立多头头寸，在空头趋势期间建立空头头寸。每当指标翻转时，当前仓位会立即平仓并在相反方向开仓。

此方法适用于趋势明显的市场，在突破后可能出现大幅波动。同时它也是学习示例，展示如何使用 `BindEx` 连接指标并在收盘后执行市价单。

## 细节

- **入场条件**：
  - 多头：SuperTrend 显示上升趋势。
  - 空头：SuperTrend 显示下降趋势。
- **多/空**：均可。
- **出场条件**：SuperTrend 给出相反信号（仓位反转）。
- **止损**：无显式止损；指标线充当跟踪止损。
- **默认值**：
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **过滤器**：
  - 类别：趋势跟随
  - 方向：多空皆可
  - 指标：SuperTrend
  - 止损：基于指标
  - 复杂度：基础
  - 时间框架：中期（默认 1 小时）
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
