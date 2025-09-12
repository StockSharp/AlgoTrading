# Trend Confirmation Strategy
[English](README.md) | [Русский](README_ru.md)

中文策略描述和交易逻辑说明。

## 细节
- **入场条件**: SuperTrend 方向与 MACD 确认，并且价格位于 VWAP 相应一侧。
- **方向**: 多空双向。
- **出场条件**: MACD 反向穿越信号线。
- **止损**: 无。
- **默认参数**: ATR 长度10，系数3，MACD 快速12，慢速26，信号9。
- **过滤器**: SuperTrend 与 VWAP。
