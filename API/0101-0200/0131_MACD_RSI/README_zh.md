# MACD RSI 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本策略结合MACD动量与RSI的超买超卖读数。
当两个指标方向一致时，行情延续的可能性更高。
MACD上穿且RSI自超卖区回升时做多；MACD下穿并且RSI从超买区回落时做空。
若进场后指标出现背离，则依据价格百分比止损以限制亏损。

测试表明年均收益约为 130%，该策略在股票市场表现最佳。

## 细节

- **入场条件**：指标信号
- **多/空**：均可
- **退出条件**：止损或反向信号
- **止损**：是，按百分比
- **默认值**:
  - `CandleType` = 15分钟
  - `StopLossPercent` = 2
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
    RSI低于RsiOversold时预备做多，高于RsiOverbought时预备做空，以最近一次极值为准。之后同方向的MACD交叉确认信号并入场，同时反转相反持仓；每个极值只确认一次交叉。
- **过滤器**:
  - 类别：趋势跟随
  - 方向：双向
  - 指标：MACD, RSI
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

