# Ichimoku by FarmerBTC 策略
[English](README.md) | [Русский](README_ru.md)

当价格位于 Ichimoku 云之上、云层呈现多头、较高周期的 SMA 确认趋势且成交量超过其移动平均乘以系数时，本策略做多。 当价格跌回云层下方时平仓。

## 细节

- **入场条件**：指标信号
- **多/空**：仅做多
- **出场条件**：反向信号
- **止损**：否
- **默认值**：
  - `TenkanPeriod` = 10
  - `KijunPeriod` = 30
  - `SenkouSpanBPeriod` = 53
  - `SmaLength` = 13
  - `VolumeLength` = 20
  - `VolumeMultiplier` = 1.5
  - `CandleType` = 1 小时
  - `HtfCandleType` = 1 天
- **筛选**：
  - 类型：趋势跟随
  - 方向：多头
  - 指标：Ichimoku、SMA、成交量
  - 止损：否
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
