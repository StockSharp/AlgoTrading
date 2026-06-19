# 趋势系统的成交量确认策略
[English](README.md) | [Русский](README_ru.md)

该策略使用趋势冲力指标（TTI）、成交量价格确认指标（VPCI）和ADX来确认多头趋势。

## 详情
- **入场条件**：
  - **多头**：ADX > 30，TTI > 信号，VPCI > 0。
- **多空方向**：仅做多。
- **出场条件**：
  - VPCI < 0。
- **止损**：无。
- **默认值**：
  - `ADX Length` = 14
  - `ADX Smoothing` = 14
  - `TTI Fast Average` = 13
  - `TTI Slow Average` = 26
  - `TTI Signal Length` = 9
  - `VPCI Short Avg` = 5
  - `VPCI Long Avg` = 25
- **过滤器**：
  - 类别：趋势
  - 方向：做多
  - 指标：ADX，TTI，VPCI
  - 止损：无
  - 复杂度：中等
  - 时间框架：中期
