# PSAR Trader 策略
[English](README.md) | [Русский](README_ru.md)

PSAR Trader 策略基于 Parabolic SAR 指标的翻转。当 SAR 点移动到价格下方时做多，移动到价格上方时做空。可选的 “Close On Opposite” 参数在出现反向信号时反手。策略只在设定的交易时段内运行，止损和止盈由保护模块自动设置。

## 详情

- **入场条件**：价格与 Parabolic SAR 的交叉。
- **多空**：双向。
- **出场条件**：相反的 SAR 交叉或反手。
- **止损**：有，固定值。
- **默认值**：
  - `SarStep` = 0.001m
  - `SarMaxStep` = 0.2m
  - `StartHour` = 0
  - `EndHour` = 23
  - `CloseOnOpposite` = true
  - `TakeValue` = 50 (绝对值)
  - `StopValue` = 50 (绝对值)
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 分类: 趋势
  - 方向: 双向
  - 指标: Parabolic SAR
  - 止损: 固定
  - 复杂度: 基础
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
