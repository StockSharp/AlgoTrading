# RVI Crossover 策略
[English](README.md) | [Русский](README_ru.md)

RVI Crossover 策略使用相对活力指数及均线过滤。
当 RVI 向上穿越信号线且价格低于 EMA 时买入；当 RVI 向下穿越信号线且价格高于 EMA 时卖出。

## 细节

- **入场条件**: RVI 与信号线交叉并结合 EMA 对 VWMA 的过滤
- **多空方向**: 双向
- **出场条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `RviLength` = 10
  - `SignalLength` = 10
  - `EmaLength` = 31
  - `VwmaLength` = 1
- **过滤器**:
  - 分类: 趋势
  - 方向: 双向
  - 指标: RVI, SMA, EMA, VWMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
