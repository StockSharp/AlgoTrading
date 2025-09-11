# Varanormal Mac N Cheez 策略
[English](README.md) | [Русский](README_ru.md)

基于SMA交叉的策略，带有追踪止损和每日盈利目标。

## 详情
- **入场条件**:
  - **做多**: 快速SMA上穿慢速SMA。
  - **做空**: 快速SMA下穿慢速SMA。
- **多空方向**: 双向
- **退出条件**:
  - 追踪止损或固定止损。
  - 达到每日盈利目标后平仓。
- **止损**: 是，固定与追踪。
- **默认值**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `DailyTarget` = 200
  - `StopLossAmount` = 100
  - `TrailOffset` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: SMA
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
