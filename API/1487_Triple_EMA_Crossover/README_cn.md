# 三重EMA交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略使用三条简单移动平均线。
当短期SMA向上穿越中期SMA且三条均线呈多头排列时开多，
反向交叉并呈空头排列时开空。
当价格重新穿过短期SMA或触发止盈止损时退出。

## 详情

- **入场条件**: SMA1和SMA2的交叉并满足趋势过滤。
- **多空方向**: 双向。
- **退出条件**: 价格穿越SMA1或保护性止损/止盈。
- **止损**: 是。
- **默认值**:
  - `Sma1Period` = 9
  - `Sma2Period` = 21
  - `Sma3Period` = 55
  - `StopLossTicks` = 200
  - `TakeProfitTicks` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类型: 趋势
  - 方向: 双向
  - 指标: SMA
  - 止损: 固定
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
