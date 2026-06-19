# Renko RSI
[English](README.md) | [Русский](README_ru.md)

该策略使用Renko砖和RSI超买/超卖信号进行交易。

测试表明该策略表现中等，适用于具有清晰Renko趋势的市场。

Renko RSI基于ATR构建Renko砖，并应用短周期RSI。当RSI上穿超卖水平时买入，当RSI跌破超买水平时卖出。

## 详情

- **入场条件**：RSI穿越超卖/超买水平。
- **多空方向**：双向。
- **出场条件**：相反信号。
- **止损**：否。
- **默认值**：
  - `RenkoAtrLength` = 14
  - `RsiLength` = 2
  - `RsiOverbought` = 80
  - `RsiOversold` = 20
  - `CandleType` = Renko ATR(14)
- **过滤器**：
  - 分类：动量
  - 方向：双向
  - 指标：RSI, Renko
  - 止损：否
  - 复杂度：基础
  - 时间框架：Renko
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中

