# 布林带反弹反转策略
[English](README.md) | [Русский](README_ru.md)

该策略在价格从布林带边缘反弹时，结合MACD和成交量确认进行交易。系统每天最多入场五次，并使用固定百分比的止损和止盈。

## 细节

- **入场条件**：
  - 多头：`Close[1] < LowerBand[1] && Close > LowerBand && MACD > Signal && Volume >= AvgVolume * VolumeFactor`
  - 空头：`Close[1] > UpperBand[1] && Close < UpperBand && MACD < Signal && Volume >= AvgVolume * VolumeFactor`
- **多空方向**：双向
- **止损**：百分比止盈和止损
- **默认参数**：
  - `BollingerPeriod` = 20
  - `BbStdDev` = 2m
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `VolumePeriod` = 20
  - `VolumeFactor` = 1m
  - `StopLossPercent` = 2m
  - `TakeProfitPercent` = 4m
  - `MaxTradesPerDay` = 5
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **过滤器**：
  - 类别：反转
  - 方向：双向
  - 指标：布林带、MACD、成交量
  - 止损：是
  - 复杂度：中等
  - 时间框架：中期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
