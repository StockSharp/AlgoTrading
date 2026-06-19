# Boilerplate Configurable 策略
[English](README.md) | [Русский](README_ru.md)

Boilerplate Configurable 策略可以在两种模式下运行：简单移动平均线交叉或布林带挤压突破。它包含交易日和交易时段过滤、日期范围、新闻窗口以及基于 ATR 或固定风险回报的风险管理。

## 详情

- **入场条件**：
  - 在 `SmaCross` 模式下，当快速 SMA 上穿慢速 SMA 时做多，反向交叉时做空。
  - 在 `Squeeze` 模式下，当价格突破外侧布林带且位于窄布林带内时入场。
- **多空方向**：可配置仅做多、仅做空或双向，并支持反转。
- **出场条件**：
  - 基于 ATR 或固定百分比的止损和止盈。
  - 每日退出时间段和新闻窗口会关闭所有持仓。
- **止损**：每笔交易的止损和止盈并带有回撤保护。
- **默认参数**：
  - `Length` = 20
  - `WideMultiplier` = 1.5
  - `NarrowMultiplier` = 2
  - `MaxLossPerc` = 0.02
  - `AtrMultiplier` = 1.5
  - `StaticRr` = 2
  - `NewsWindow` = 5
  - `MaxDrawdown` = 0.1
- **过滤器**：
  - 类别：模块化
  - 方向：多头和空头
  - 指标：SMA、布林带、ATR
  - 止损：是
  - 复杂度：高
  - 时间框架：任意
  - 季节性：是
  - 神经网络：否
  - 背离：否
  - 风险级别：高
