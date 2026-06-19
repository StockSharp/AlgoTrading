# 波动捕捉 RSI-Bollinger
[English](README.md) | [Русский](README_ru.md)

该策略利用动态布林带并可选用 RSI 过滤器以捕捉波动性。

## 详情
- **入场条件**：价格穿越自适应布林带，可选择 RSI 确认。
- **多空方向**：通过 `Direction` 参数配置。
- **退出条件**：价格穿越相反方向的跟踪带。
- **止损**：无。
- **默认值**：
  - `BollingerLength` = 50
  - `Multiplier` = 2.7183m
  - `UseRsi` = true
  - `RsiPeriod` = 10
  - `RsiSmaPeriod` = 5
  - `BoughtRangeLevel` = 55m
  - `SoldRangeLevel` = 50m
  - `Direction` = TradeDirection.Both
  - `CandleType` = TimeSpan.FromMinutes(5)
- **过滤器**：
  - 类型：波动性
  - 方向：可配置
  - 指标：Bollinger、RSI
  - 止损：无
  - 复杂度：基础
  - 时间框架：日内 (5m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
