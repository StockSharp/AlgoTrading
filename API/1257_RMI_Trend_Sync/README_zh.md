# RMI 趋势同步
[English](README.md) | [Русский](README_ru.md)

RMI 趋势同步结合 RSI 与 MFI 的动量信号，并使用 SuperTrend 作为追踪止损。平均动量向上突破阈值且 EMA 斜率上升时做多，向下突破时做空。退出基于相反动量或 SuperTrend 线。

## 详情

- **入场条件**: 平均动量与 EMA 斜率确认的阈值突破。
- **多空方向**: 两个方向。
- **出场条件**: 相反动量或 SuperTrend 停损。
- **止损**: 有。
- **默认值**:
  - `RmiLength` = 21
  - `PositiveThreshold` = 70
  - `NegativeThreshold` = 30
  - `SuperTrendLength` = 10
  - `SuperTrendMultiplier` = 3.5
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**:
  - 类别: Trend
  - 方向: Both
  - 指标: RSI, MFI, EMA, SuperTrend
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
