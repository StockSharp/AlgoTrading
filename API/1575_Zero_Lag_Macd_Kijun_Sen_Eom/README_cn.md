# 零滞后MACD + 基准线 + EOM 策略
[English](README.md) | [Русский](README_ru.md)

该策略将零滞后MACD、基准线(Kijun-sen)与Ease of Movement结合，并基于ATR设置止损和止盈。

## 详情

- **入场条件**：MACD金叉/死叉并满足基准线和EOM过滤。
- **多空方向**：双向。
- **退出条件**：基于ATR的止损或止盈。
- **止损**：是。
- **默认值**：
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `MacdEmaLength` = 9
  - `KijunPeriod` = 26
  - `EomLength` = 14
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.5m
  - `RiskReward` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类型: 趋势
  - 方向: 双向
  - 指标: MACD, Donchian, EOM, ATR
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内 (5m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
