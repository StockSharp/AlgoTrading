# Breakeven Trailing Stop 策略
[English](README.md) | [Русский](README_ru.md)

该策略演示如何先将止损移动到保本位置，然后随着价格上涨继续跟踪。
在开多仓后，策略分两阶段管理仓位：
1. 当价格上涨 `BreakevenPlus` 点时，止损被移动到入场价上方 `BreakevenStep` 点。
2. 如果价格进一步距离当前止损 `TrailingPlus` 点，止损将以距离 `TrailingStep` 点的方式跟随价格。

若手动开空仓，逻辑同样适用。

## 细节

- **入场条件**：第一根完成的K线开多。
- **多/空方向**：均可（示例使用多头）。
- **出场条件**：价格触及跟踪止损。
- **止损**：保本止损和跟踪止损。
- **默认参数**：
  - `BreakevenPlus` = 5
  - `BreakevenStep` = 2
  - `TrailingPlus` = 3
  - `TrailingStep` = 1
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选器**：
  - 类别：止损管理
  - 方向：双向
  - 指标：无
  - 止损：保本、跟踪
  - 复杂度：基础
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
