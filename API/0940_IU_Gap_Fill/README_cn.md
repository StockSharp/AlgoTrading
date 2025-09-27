# IU Gap Fill Strategy
[English](README.md) | [Русский](README_ru.md)

IU Gap Fill 策略在价格与上一交易日收盘价形成缺口并回补该缺口时入场。向上跳空后回测并收盘价重新站上前收盘价时做多；向下跳空后反弹并收盘价跌回前收盘价下方时做空。离场使用基于 ATR 的移动止损。

## 详情
- **数据**: 指定时间框的K线。
- **入场条件**:
  - **多头**: 向上跳空至少 `GapPercent` 且价格上穿前一日收盘价。
  - **空头**: 向下跳空至少 `GapPercent` 且价格跌破前一日收盘价。
- **出场条件**: ATR 移动止损。
- **止损**: `AtrLength` × `AtrFactor` 的 ATR 轨迹。
- **默认值**:
  - `CandleType` = 1m
  - `GapPercent` = 0.2
  - `AtrLength` = 14
  - `AtrFactor` = 2
- **过滤器**:
  - 类别: 缺口
  - 方向: 多 & 空
  - 指标: ATR
  - 复杂度: 低
  - 风险水平: 中
