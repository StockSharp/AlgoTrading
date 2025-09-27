# Smart Money Pivot Strategy
[English](README.md) | [Русский](README_ru.md)

该策略利用枢轴高点和低点的突破进行交易。价格突破最新枢轴高点时做多，跌破枢轴低点时做空。每笔交易都有独立的止损和止盈百分比。

## 细节

- **入场条件**：向上突破枢轴高点或向下突破枢轴低点。
- **多/空**：双向。
- **出场条件**：止损或止盈。
- **止损**：有。
- **默认值**：
  - `EnableLongStrategy` = true
  - `LongStopLossPercent` = 1m
  - `LongTakeProfitPercent` = 1.5m
  - `EnableShortStrategy` = true
  - `ShortStopLossPercent` = 1m
  - `ShortTakeProfitPercent` = 1.5m
  - `Period` = 20
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别：突破
  - 方向：双向
  - 指标：价格行为
  - 止损：有
  - 复杂度：基础
  - 时间框架：日内 (1m)
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
