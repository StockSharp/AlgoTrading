# Fibo iSAR 策略
[English](README.md) | [Русский](README_ru.md)

该策略结合快慢两条 Parabolic SAR 指标与斐波那契回撤。当快速 SAR 位于慢速 SAR 之上且低于价格时，视为上升趋势，在最近区间的 50% 斐波那契回撤位挂入 Buy Limit 订单。止损放在近期低点下方，止盈设在 161% 扩展位。下降趋势时逻辑相反，使用 Sell Limit 订单。

## 详情

- **入场条件**: 通过快/慢 SAR 判断趋势方向，按 50% 斐波那契回撤入场。
- **多空方向**: 双向。
- **退出条件**: 止损或止盈触发。
- **止损**: 有。
- **默认值**:
  - `StepFast` = 0.02
  - `MaximumFast` = 0.2
  - `StepSlow` = 0.01
  - `MaximumSlow` = 0.1
  - `CountBarSearch` = 3
  - `IndentStopLoss` = 30
  - `FiboEntranceLevel` = 50
  - `FiboProfitLevel` = 161
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: Parabolic SAR, 斐波那契
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
