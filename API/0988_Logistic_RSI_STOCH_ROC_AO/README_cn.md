# Logistic RSI STOCH ROC AO
[English](README.md) | [Русский](README_ru.md)

该策略对选择的指标（AO、ROC、RSI、Stochastic）应用逻辑映射，当带符号的标准差穿越零值时进行交易。

## 细节

- **入场条件**：带符号标准差上穿零。
- **多空方向**：双向。
- **出场条件**：带符号标准差下穿零。
- **止损**：无。
- **默认值**：
  - `Indicator` = LogisticDominance
  - `Length` = 13
  - `LenLd` = 5
  - `LenRoc` = 9
  - `LenRsi` = 14
  - `LenSto` = 14
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：Oscillator
  - 方向：双向
  - 指标：AwesomeOscillator, RateOfChange, RelativeStrengthIndex, StochasticOscillator, Highest
  - 止损：无
  - 复杂度：中等
  - 时间框架：日内 (1m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险级别：中等
