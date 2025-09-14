# Step Stochastic Cross Strategy

## 概述
该策略使用基于ATR的自定义 Step Stochastic 指标来生成反转信号。策略订阅所选时间框的K线，并计算从0到100的快线和慢线。

## 入场与出场规则
- **做多入场：** 慢线高于50且快线从上向下穿越慢线。
- **做空入场：** 慢线低于50且快线从下向上穿越慢线。
- **做多出场：** 慢线低于50并允许平多。
- **做空出场：** 慢线高于50并允许平空。

## 参数
- `KFast` – 快速通道乘数。
- `KSlow` – 慢速通道乘数。
- `CandleType` – K线时间框。
- `AllowBuyOpen`、`AllowSellOpen`、`AllowBuyClose`、`AllowSellClose` – 交易操作权限。
- `StopLoss`、`TakeProfit` – 以价格单位表示的可选保护水平。

策略在需要时调用 `StartProtection` 应用止损和止盈。`StepStochasticIndicator` 为原始 MQL5 指标的 C# 移植，输出每根完成K线的快线和慢线。
