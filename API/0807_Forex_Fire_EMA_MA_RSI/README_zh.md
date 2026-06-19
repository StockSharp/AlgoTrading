# Forex Fire EMA MA RSI 策略
[English](README.md) | [Русский](README_ru.md)

多时间框架趋势策略，结合 EMA、MA 和 RSI。使用4小时K线确认，15分钟K线入场。

## 细节

- **入场条件**：
  - 多头：短期 EMA 高于长期 EMA，价格高于 MA，快 RSI 高于慢 RSI 且 >50，成交量增加，并有高时间框架确认。
  - 空头：条件相反。
- **多空方向**：双向。
- **出场条件**：
  - EMA 交叉或 RSI 达到阈值。
  - 可选止损、止盈、追踪止损和 ATR 退出。
- **止损**：支持，可配置。
- **默认值**：
  - `EmaShortLength` = 13
  - `EmaLongLength` = 62
  - `MaLength` = 200
  - `MaType` = MovingAverageTypeEnum.Simple
  - `RsiSlowLength` = 28
  - `RsiFastLength` = 7
  - `RsiOverbought` = 70
  - `RsiOversold` = 30
  - `UseStopLoss` = true
  - `StopLossPercent` = 2
  - `UseTakeProfit` = true
  - `TakeProfitPercent` = 4
  - `UseTrailingStop` = true
  - `TrailingPercent` = 1.5
  - `UseAtrExits` = true
  - `AtrMultiplier` = 2
  - `AtrLength` = 14
  - `EntryCandleType` = TimeSpan.FromMinutes(15).TimeFrame()
  - `ConfluenceCandleType` = TimeSpan.FromHours(4).TimeFrame()
- **过滤器**：
  - 类别: 趋势
  - 方向: 双向
  - 指标: EMA, MA, RSI, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 多时间框架
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
