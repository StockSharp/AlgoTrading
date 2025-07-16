# VWAP 回归
[English](README.md) | [Русский](README_ru.md)

该策略在价格偏离成交量加权平均价(VWAP)较远时逆势交易，待价格回归后离场。由于VWAP代表典型成交水平，极端偏离往往吸引价格回到其附近，常与日内趋势过滤结合提高胜率。

## 详情
- **入场条件**: 基于 RSI、VWAP 的信号
- **多空方向**: 双向
- **退出条件**: 反向信号或止损
- **止损**: 是
- **默认值**:
  - `DeviationPercent` = 2.0m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 均值回归
  - 方向: 双向
  - 指标: RSI, VWAP
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
