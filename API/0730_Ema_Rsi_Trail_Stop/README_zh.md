# EMA RSI Trailing Stop 策略
[Русский](README_ru.md) | [English](README.md)

该策略在短期 EMA 穿越中期 EMA 且位于长期 EMA 之上或之下时开仓。通过 RSI 水平和平移止损结合固定百分比止损退出。若交易在盈利，可在指定 K 线数量后平仓。

## 详情

- **入场条件**：EMA A 与 EMA B 金叉/死叉并由 EMA C 确认趋势，且 K 线方向一致。
- **多空方向**：双向。
- **出场条件**：RSI 阈值、移动止损或时间退出。
- **止损**：固定百分比止损，当价格向有利方向移动 `TrailOffset` 后转为移动止损。
- **默认参数**：
  - `EmaALength` = 10
  - `EmaBLength` = 20
  - `EmaCLength` = 100
  - `RsiLength` = 14
  - `ExitLongRsi` = 70
  - `ExitShortRsi` = 30
  - `TrailPoints` = 50
  - `TrailOffset` = 10
  - `FixStopLossPercent` = 5
  - `CloseAfterXBars` = true
  - `XBars` = 24
  - `ShowLong` = true
  - `ShowShort` = false
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：EMA, RSI
  - 止损：移动
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
