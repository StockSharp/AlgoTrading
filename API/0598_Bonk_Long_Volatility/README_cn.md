# BONK Long Volatility
[English](README.md) | [Русский](README_ru.md)

该策略仅做多，结合均线、波动性和成交量过滤。当市场上行、波动扩大并且动量指标确认时买入。离场通过固定止盈、止损以及基于ATR的追踪止损完成。

## 详情

- **入场条件**: 快速均线高于慢速均线，K线范围大于 ATR * `AtrMultiplier`，RSI 在 `RsiOversold` 与 `RsiOverbought` 之间，MACD 线高于信号线且大于0，成交量高于均量 * `VolumeThreshold`，收盘价高于快速均线，K线时间在最近 `LookbackDays` 天内。
- **多空方向**: 仅多头。
- **出场条件**: 止盈、止损或ATR追踪止损。
- **止损**: 有。
- **默认值**:
  - `ProfitTargetPercent` = 5.0m
  - `StopLossPercent` = 3.0m
  - `AtrLength` = 10
  - `AtrMultiplier` = 1.5m
  - `RsiLength` = 14
  - `RsiOverbought` = 65
  - `RsiOversold` = 35
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `VolumeSmaLength` = 20
  - `VolumeThreshold` = 1.5m
  - `MaFastLength` = 5
  - `MaSlowLength` = 13
  - `LookbackDays` = 30
  - `CandleType` = TimeSpan.FromHours(1)
- **过滤器**:
  - 类别: Trend
  - 方向: Long
  - 指标: SMA, ATR, RSI, MACD, Volume
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等

