# 带可调RSI和止损的RSI策略
[English](README.md) | [Русский](README_ru.md)

当RSI跌破阈值时买入，当价格突破前一根K线的高点时平仓。每笔交易都使用百分比止损进行保护。

## 详情

- **入场条件**:
  - 多头：RSI低于 `RsiThreshold`
- **多空方向**: 多头
- **出场条件**:
  - 收盘价高于前一根K线的最高价
  - 触发止损
- **止损**: 是
- **默认值**:
  - `RsiLength` = 8
  - `RsiThreshold` = 28m
  - `StopLossPercent` = 5m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: 振荡指标
  - 方向: 多头
  - 指标: RSI
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 短期
  - 季节性: 无
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

