# Fxscalper 策略
[English](README.md) | [Русский](README_ru.md)

从 MQL4 专家“fxscalper”移植的布林带突破剥头皮策略。
策略订阅蜡烛数据和 Bollinger Bands。当收盘价突破上轨时开多仓；当收盘价跌破下轨时开空仓。仓位通过止损和止盈进行保护。

## 详情

- **入场条件**:
  - 多头: `Close > Upper Band`
  - 空头: `Close < Lower Band`
- **做多/做空**: 同时
- **离场条件**: 反向信号或保护性止损
- **止损**: 止损和止盈
- **默认参数**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2
  - `StopLoss` = 200m
  - `TakeProfit` = 150m
- **过滤器**:
  - 类别: Bollinger Bands
  - 方向: 双向
  - 指标: Bollinger Bands
  - 止损: 是
  - 复杂度: 基础
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险水平: 中等
