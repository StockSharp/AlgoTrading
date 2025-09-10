# BB RSI Trailing Stop 策略
[English](README.md) | [Русский](README_ru.md)

将布林带与RSI动量结合，并在利润达到指定阈值后启用跟踪止损。
当价格跌破下轨且RSI从超卖区上穿时做多；价格突破上轨且RSI从超买区下穿时做空。

初始止损为固定点数，当价格向有利方向移动并超过偏移量时，止损转换为跟踪模式。

## 细节

- **入场条件**：布林带突破并由RSI确认
- **多空**：双向
- **出场条件**：初始止损或跟踪止损
- **止损**：是，动态跟踪
- **默认参数**：
  - `BollingerPeriod` = 25
  - `BollingerDeviation` = 2
  - `RsiPeriod` = 14
  - `RsiOverbought` = 60
  - `RsiOversold` = 33
  - `StopLossPoints` = 50
  - `TrailOffsetPoints` = 99
  - `TrailStopPoints` = 40
- **过滤器**：
  - 类别：均值回归
  - 方向：双向
  - 指标：布林带、RSI
  - 止损：跟踪
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
