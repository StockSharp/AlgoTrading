# OBVious MA 策略
[English](README.md) | [Русский](README_ru.md)

当 OBV 上穿其多头进入均线时，策略开多；当 OBV 下穿多头退出均线时平多。OBV 下穿空头进入均线时开空，在其上穿空头退出均线时平空。方向过滤器可仅启用多头或仅启用空头。

## 细节

- **入场条件**:
  - **多头**: OBV 上穿多头进入均线且方向不是 "Short"。
  - **空头**: OBV 下穿空头进入均线且方向不是 "Long"。
- **多/空**: 都可
- **出场条件**:
  - 多头: OBV 下穿多头退出均线。
  - 空头: OBV 上穿空头退出均线。
- **止损**: 无。
- **默认值**:
  - `LongEntryLength` = 190
  - `LongExitLength` = 202
  - `ShortEntryLength` = 395
  - `ShortExitLength` = 300
  - `TradeDirection` = "Long"
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 都可
  - 指标: OBV, SMA
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 低
