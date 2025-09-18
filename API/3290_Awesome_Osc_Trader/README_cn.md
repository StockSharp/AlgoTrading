# Awesome Osc Trader 策略
[English](README.md) | [Русский](README_ru.md)

该策略复现 MetaTrader 上的 "Awesome Osc Trader" 专家顾问，通过布林带宽度、随机指标过滤以及归一化的 Awesome Oscillator 动能来确定信号。当振荡器在负值区域回升且随机指标脱离超卖区域，并且市场波动率保持在设定区间内时开多；开空则需要相反条件。可配置的交易时间窗口限制新开仓的时段，已开仓位可在出现反向信号且浮动盈亏满足所选过滤器时被强制平仓。

## 详情

- **入场条件**：
  - 布林带上轨与下轨之间的价差（换算成点数）必须处于 `BollingerSpreadLowerLimit` 与 `BollingerSpreadUpperLimit` 之间。
  - 随机指标主线多头需高于 `StochLower`，空头需低于 `StochUpper`。
  - 归一化后的 Awesome Oscillator 至少连续四根柱位于零线相反侧，并以绝对值大于 `AoStrengthLimit` 的力度向零轴回转。
  - 当前时间位于 `EntryHour` 与 `OpenHours` 定义的交易窗口中。
- **方向**：可做多亦可做空。
- **出场条件**：
  - 可选的反向信号或振荡器过零离场，由 `CloseTrade` 和 `ProfitTypeClTrd` 控制，后者限定仅在指定盈亏状态下平仓。
  - 止损、止盈与追踪止损距离以点数设置。
- **止损管理**：通过 `StartProtection` 应用固定止损/止盈以及可选的追踪止损。
- **默认参数**：
  - `BollingerPeriod` = 20，`BollingerSigma` = 2
  - `BollingerSpreadLowerLimit` = 55，`BollingerSpreadUpperLimit` = 380
  - `PeriodFast` = 3，`PeriodSlow` = 32
  - `AoStrengthLimit` = 0.13
  - `StochK` = 8，`StochD` = 3，`StochSlow` = 3
  - `StochLower` = 18，`StochUpper` = 76
  - `EntryHour` = 0，`OpenHours` = 16
  - `Lots` = 0.01，`TakeProfit` = 200，`StopLoss` = 80，`TrailingStop` = 40
  - `CloseTrade` = true，`ProfitTypeClTrd` = 1（仅平掉盈利仓位）
- **过滤标签**：
  - 分类：动量 + 波动率过滤
  - 方向：双向
  - 指标：布林带、随机指标、Awesome Oscillator
  - 止损：支持固定和追踪
  - 复杂度：中等
  - 周期：原策略建议 H1，可按需切换其他蜡烛序列
  - 季节性：使用交易时间窗口
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
