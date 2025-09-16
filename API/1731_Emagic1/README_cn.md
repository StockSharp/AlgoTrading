# EMA MACD 信号趋势策略
[English](README.md) | [Русский](README_ru.md)

当快速 EMA 高于慢速 EMA 且 MACD 信号线向上时，该策略做多；当快速 EMA 低于慢速 EMA 且信号线向下时做空。可选地使用止损、止盈和追踪止损。

## 细节

- **入场条件**:
  - 快速 EMA > 慢速 EMA 且 MACD 信号线上升 → 买入。
  - 快速 EMA < 慢速 EMA 且 MACD 信号线下降 → 卖出。
- **出场条件**:
  - 相反信号关闭仓位。
- **指标**: EMA, MACD signal。
- **类型**: 趋势跟随。
- **时间框架**: 默认 5 分钟。
