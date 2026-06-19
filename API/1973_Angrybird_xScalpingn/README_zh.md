# Angrybird xScalpingn
[English](README.md) | [Русский](README_ru.md)

Angrybird xScalpingn 是一种马丁格尔式剥头皮策略。首笔交易根据最近的收盘方向和 RSI 过滤器入场。当价格逆着持仓按最近区间计算的动态步长移动时，策略会按系数放大手数加仓。CCI 出现强烈反向信号或触发止损/止盈时，所有仓位都会平掉。

## 细节

- **入场条件**：首笔交易跟随最近的收盘方向并满足 RSI 过滤。价格逆势达到计算步长时加仓。
- **多空**：双向。
- **出场条件**：CCI 反向或保护性止损/止盈。
- **止损**：有。
- **默认值**：
  - `Volume` = 0.01
  - `LotExponent` = 2
  - `DynamicPips` = true
  - `DefaultPips` = 12
  - `Depth` = 24
  - `Del` = 3
  - `TakeProfit` = 20
  - `StopLoss` = 500
  - `Drop` = 500
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `MaxTrades` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **过滤器**：
  - 类别: Grid
  - 方向: 双向
  - 指标: RSI, CCI
  - 止损: 有
  - 复杂度: 高
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 高
