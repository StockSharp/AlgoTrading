# Follow Line Strategy
[English](README.md) | [Русский](README_ru.md)

该策略基于布林带突破构建跟随线，并可选择使用ATR偏移。 当跟随线方向改变时入场，可选择通过更高时间框架趋势确认。

## 细节

- **入场条件**：价格突破布林带后跟随线转向，可选更高时间框架确认。
- **多空方向**：双向。
- **出场条件**：跟随线或高时间框架趋势反转。
- **止损**：无。
- **默认值**：
  - `AtrPeriod` = 5
  - `BbPeriod` = 21
  - `BbDeviation` = 1
  - `UseAtrFilter` = true
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `UseHtfConfirmation` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HtfCandleType` = TimeSpan.FromHours(4)
- **过滤器**：
  - 类别: Trend
  - 方向: Both
  - 指标: Bollinger Bands, ATR
  - 止损: 无
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: Medium
