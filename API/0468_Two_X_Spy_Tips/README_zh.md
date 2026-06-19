# Two X SPY TIPS
[English](README.md) | [Русский](README_ru.md)

当 S&P 500 和 TIPS 的价格在新月份开始时都高于各自的 200 期 SMA 时，该策略会在交易品种上建仓。

## 细节

- **入场条件**：S&P 500 与 TIPS 高于各自的 SMA 且出现新月份。
- **多空方向**：仅做多。
- **出场条件**：无。
- **止损**：无。
- **默认值**：
  - `SmaLength` = 200
  - `Leverage` = 2
  - `CandleType` = TimeSpan.FromDays(1)
- **过滤器**：
  - 类别: 趋势
  - 方向: 仅多
  - 指标: SMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日线
  - 季节性: 是
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
