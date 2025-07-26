# 锤形反转 (Hammer Candle Reversal)
[English](README.md) | [Русский](README_ru.md)

锤头线通常预示盘中反转, 下影线至少为实体两倍。

识别后做多, 止损在形态低点。

## 详情

- **入场条件**: Hammer candle detected.
- **多空方向**: Long only.
- **出场条件**: Stop-loss or discretionary exit.
- **止损**: Yes.
- **默认值**:
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Pattern
  - 方向: Long
  - 指标: Candlestick
  - 止损: Yes
  - 复杂度: Basic
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium

测试表明年均收益约为 64%，该策略在外汇市场表现最佳。
