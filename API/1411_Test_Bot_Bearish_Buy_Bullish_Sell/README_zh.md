# Test Bot: Bearish Buy / Bullish Sell
[Русский](README_ru.md) | [English](README.md)

在无持仓时遇到第一根看跌K线买入，在第一根看涨K线平仓。

## 细节

- **入场条件**: 空仓且出现第一根看跌K线。
- **多空方向**: 仅做多。
- **出场条件**: 第一根看涨K线。
- **止损**: 无。
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤**:
  - 类别: 反转
  - 方向: 多
  - 指标: 无
  - 止损: 无
  - 复杂度: 基础
  - 周期: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险: 中等
