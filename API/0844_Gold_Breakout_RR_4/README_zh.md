# Gold Breakout RR4 策略
[English](README.md) | [Русский](README_ru.md)

Gold Breakout RR4 策略在黄金上使用 Donchian 通道突破，并结合成交量和 LWTI 趋势过滤。每天只交易一次，在指定时段内执行，并采用固定的 4:1 风险收益比。

## 细节

- **入场条件**：价格在会话内突破 Donchian 通道且成交量高于均值并得到 LWTI 确认
- **多/空**：双向
- **出场条件**：固定风险收益的止损和止盈
- **止损**：是
- **默认值**：
  - `DonchianLength` = 96
  - `MaVolumeLength` = 30
  - `LwtiLength` = 25
  - `LwtiSmooth` = 5
  - `StartHour` = 20
  - `EndHour` = 8
  - `RiskReward` = 4
- **筛选**：
  - 类别：突破
  - 方向：双向
  - 指标：Donchian Channel, SMA, WMA
  - 止损：是
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：无
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
