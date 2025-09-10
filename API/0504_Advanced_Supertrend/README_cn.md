# 高级Supertrend策略
[English](README.md) | [Русский](README_ru.md)

高级Supertrend策略在经典Supertrend指标基础上加入RSI、均线和趋势强度等可选过滤器。Supertrend由空翻多时做多，由多翻空时做空。可选的止损和止盈基于ATR倍数。

## 细节

- **入场条件**：
  - Supertrend方向变化（空→多做多，多→空做空）。
  - 可选过滤：RSI在设定区间内、价格相对均线、趋势强度和突破确认。
- **方向**：多空双向。
- **出场条件**：
  - 反向Supertrend信号或可选的止损/止盈。
- **止损**：可选的ATR倍数止损与止盈。
- **默认参数**：
  - `AtrLength` = 6
  - `Multiplier` = 3.0
  - `UseRsiFilter` = false
  - `RsiLength` = 14
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseMaFilter` = true
  - `MaLength` = 50
  - `MaType` = Weighted
  - `UseStopLoss` = true
  - `SlMultiplier` = 3.0
  - `UseTakeProfit` = true
  - `TpMultiplier` = 9.0
  - `UseTrendStrength` = false
  - `MinTrendBars` = 2
  - `UseBreakoutConfirmation` = true
- **过滤器**：
  - 类型：趋势跟随
  - 方向：多空
  - 指标：Supertrend、RSI、均线
  - 止损：基于ATR
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
