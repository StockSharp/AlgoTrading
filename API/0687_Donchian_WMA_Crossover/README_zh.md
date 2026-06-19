# Donchian WMA Crossover 策略
[English](README.md) | [Русский](README_ru.md)

当 Donchian 通道下轨向上穿越加权移动平均线时，仅在 2025 年内做多。达到设定的盈利百分比、在 WMA 下行时下轨反向穿越，或日期不在 2025 年时全部平仓。

## 详情

- **入场条件**：
  - 做多：`DonchianLow` 上穿 `WMA` 且日期在 2025 年内
- **多空方向**：仅做多
- **出场条件**：
  - 通过 `TakeProfitPercent` 的盈利目标
  - `DonchianLow` 下穿 `WMA` 且 `WMA` 下降
  - 日期不在 2025 年
- **止损**：仅盈利目标
- **默认值**：
  - `DonchianLength` = 7
  - `WmaLength` = 62
  - `TakeProfitPercent` = 0.01m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 分类：突破
  - 方向：多头
  - 指标：Donchian 通道、加权移动平均线
  - 止损：是
  - 复杂度：初级
  - 时间框架：中期
  - 季节性：仅 2025 年
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
