# 多条件曲线拟合策略
[English](README.md) | [Русский](README_ru.md)

结合EMA交叉、RSI和随机震荡指标，在多个信号一致时进行交易。

## 细节

- **入场条件**:
  - 做多: `FastEMA > SlowEMA` 且 `RSI < RsiOversold` 且 `StochK < 20`
  - 做空: `FastEMA < SlowEMA` 且 `RSI > RsiOverbought` 且 `StochK > 80`
- **多空方向**: 双向
- **出场条件**:
  - 多单: `FastEMA < SlowEMA` 或 `RSI > RsiOverbought` 或 `StochK > StochD`
  - 空单: `FastEMA > SlowEMA` 或 `RSI < RsiOversold` 或 `StochK < StochD`
- **止损**: 无
- **默认值**:
  - `FastEmaLength` = 10
  - `SlowEmaLength` = 25
  - `RsiLength` = 14
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `StochLength` = 14
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: EMA, RSI, Stochastic
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 短期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等
