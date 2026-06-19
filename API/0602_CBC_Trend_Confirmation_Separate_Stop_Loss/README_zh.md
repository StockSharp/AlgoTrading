# CBC 策略：趋势确认与独立止损
[English](README.md) | [Русский](README_ru.md)

该策略利用蜡烛颜色变化 (CBC) 状态识别突破前一根蜡烛的高点或低点，并通过 EMA 与 VWAP 进行趋势确认，仅在指定时间窗口内入场。退出时根据 ATR 乘数设置止盈，并在突破前一根蜡烛的极值时止损。

## 详情

- **入场条件**：CBC 翻转，可选强烈翻转过滤，慢 EMA 相对 VWAP，限定交易时间。
- **多空方向**：双向。
- **出场条件**：ATR 乘数止盈，前一根蜡烛高/低点止损。
- **止损**：有。
- **默认值**：
  - `AtrLength` = 14
  - `ProfitTargetMultiplier` = 1.0m
  - `StrongFlipsOnly` = true
  - `EntryStartHour` = 10
  - `EntryEndHour` = 15
  - `CandleType` = TimeSpan.FromMinutes(5)
- **筛选器**：
  - 分类：趋势
  - 方向：双向
  - 指标：EMA, VWAP, ATR
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
