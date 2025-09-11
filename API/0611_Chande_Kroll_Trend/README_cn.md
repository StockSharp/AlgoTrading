# Chande Kroll Trend 策略
[English](README.md) | [Русский](README_ru.md)

该策略利用 Chande Kroll 停损和 SMA 趋势过滤器。当收盘价上穿下方停损并高于 SMA 时做多。收盘价跌破上方停损时平仓。仓位大小根据 1560 根K线中的最低收盘价和风险倍数计算。

## 细节

- **入场条件**：
  - 多头：`前一收盘 <= 前一低停损 && 收盘 > 低停损 && 收盘 > SMA`
- **方向**：仅做多
- **出场条件**：
  - 多头：`收盘 < 高停损`
- **止损**：Chande Kroll 停损（Donchian 极值 ± ATR）
- **默认值**：
  - `CalcMode` = CalcMode.Exponential
  - `RiskMultiplier` = 5m
  - `AtrPeriod` = 10
  - `AtrMultiplier` = 3m
  - `StopLength` = 21
  - `SmaLength` = 21
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **筛选**：
  - 类型：Trend
  - 方向：Long
  - 指标：ATR, Donchian, SMA, Lowest
  - 止损：有
  - 复杂度：Beginner
  - 时间框架：Mid-term
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：Medium
