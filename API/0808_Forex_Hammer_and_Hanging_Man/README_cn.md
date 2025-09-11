# Forex 锤子线与上吊线
[English](README.md) | [Русский](README_ru.md)

该策略交易经典的蜡烛图反转形态: 看涨锤子线和看跌上吊线。出现锤子线时做多, 出现上吊线时做空, 并持有固定数量的K线。

当持有期结束或保护性止损/止盈触发时, 头寸被平仓。

## 详情

- **入场条件**: Hammer for long, Hanging Man for short.
- **多空方向**: Both.
- **出场条件**: Hold period or stop-loss/take-profit.
- **止损**: Yes.
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `BodyLengthMultiplier` = 5
  - `ShadowRatio` = 1
  - `HoldPeriods` = 26
- **过滤器**:
  - 类别: Pattern
  - 方向: Both
  - 指标: Candlestick
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
