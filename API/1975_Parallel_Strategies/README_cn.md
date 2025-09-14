# Parallel Strategies 策略 (中文)
[English](README.md) | [Русский](README_ru.md)

基于 Heikin Ashi 和 MACD 的突破系统，可做多也可做空。当 Heikin Ashi 趋势反转并且价格突破 Donchian 通道，同时 MACD 方向一致时进场。

Heikin Ashi 用于识别趋势方向，Donchian 通道用于寻找突破，MACD 过滤动量不足的信号。

适合寻找趋势反转后早期突破的交易者，主要用于日内周期。

## 细节

- **入场条件**：
  - 多头：`趋势转多 && Close > DonchianHigh && MACD > Signal`
  - 空头：`趋势转空 && Close < DonchianLow && MACD < Signal`
- **多空方向**：双向
- **出场条件**：
  - 相反的突破信号
- **止损**：未设置
- **默认参数**：
  - `DonchianPeriod` = 5
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤**：
  - 类型：突破
  - 方向：双向
  - 指标：Heikin Ashi、Donchian Channel、MACD
  - 止损：无
  - 复杂度：中等
  - 周期：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
