# Hybrid EA 策略
[English](README.md) | [Русский](README_ru.md)

Hybrid EA 策略使用相对活力指数 (RVI) 及其信号线。
当 RVI 高于信号线设定的差值时买入，当 RVI 低于信号线相同的差值时卖出。仓位通过固定的止盈和止损点数进行保护。

## 细节

- **入场条件**: RVI 减信号超过阈值
- **多空方向**: 双向
- **出场条件**: 反向阈值交叉或止盈/止损
- **止损**: 有，固定点数
- **默认值**:
  - `Volume` = 1
  - `RviLength` = 10
  - `SignalLength` = 4
  - `DifferenceThreshold` = 0.05
  - `TakeProfit` = 18
  - `StopLoss` = 9
  - `CandleType` = 5 分钟 K 线
- **过滤器**:
  - 分类: 振荡器
  - 方向: 双向
  - 指标: RVI, SMA
  - 止损: 有
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
