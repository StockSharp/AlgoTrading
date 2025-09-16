# Ha MaZi
[English](README.md) | [Русский](README_ru.md)

该策略结合 Heikin Ashi 蜡烛、EMA 滤波和 ZigZag 转折确认。当在 EMA 上方出现新的 ZigZag 低点且 HA 蜡烛转多时开多；当在 EMA 下方出现新的 ZigZag 高点且 HA 蜡烛转空时做空。仓位通过固定止损或止盈平仓。

## 细节
- **入场条件**: ZigZag 转折与 HA 方向一致并由 EMA 过滤。
- **多空方向**: 双向。
- **出场条件**: 止损或止盈。
- **止损**: 固定止损和目标。
- **默认值**:
  - `MaPeriod` = 40
  - `ZigzagLength` = 13
  - `StopLoss` = 70
  - `TakeProfit` = 200
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: Heikin Ashi, EMA, ZigZag
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
