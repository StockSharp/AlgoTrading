# ADX CCI MA
[English](README.md) | [Русский](README_ru.md)

该策略结合 ADX、CCI 和可配置移动平均线以交易强势趋势。

当 +DI 上穿 -DI 且 CCI > 100、ADX 高于阈值时做多（若启用 MA 过滤器则收盘价需高于 MA）；当 -DI 上穿 +DI 且 CCI < -100、ADX 高于阈值时做空（收盘价低于 MA）。

包含百分比止损和止盈，可选 MA 风险管理：连续多根 K 线与 MA 方向相反时平仓。

## 细节

- **入场条件**：+DI/-DI 交叉并伴随 CCI 极值且 ADX > `AdxThreshold`，可选与 MA 的位置。
- **多空方向**：双向。
- **出场条件**：触发止损或止盈，可选 MA 风险管理。
- **止损**：是，止盈和止损。
- **默认值**：
  - `EnableLong` = true
  - `EnableShort` = true
  - `TakeProfitPercent` = 2m
  - `StopLossPercent` = 1m
  - `CciPeriod` = 15
  - `AdxLength` = 10
  - `AdxThreshold` = 20m
  - `UseMaTrend` = true
  - `MaType` = MovingAverageTypeEnum.Simple
  - `MaLength` = 200
  - `UseMaRiskManagement` = false
  - `MaRiskExitCandles` = 2
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类别：趋势
  - 方向：双向
  - 指标：ADX, CCI, MA
  - 止损：是
  - 复杂度：中
  - 时间框架：日内 (5m)
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中
