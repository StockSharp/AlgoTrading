# AMMA趋势策略

## 概述

该策略使用 **Modified Moving Average (AMMA)** 指标来捕捉短期趋势变化。通过分析最近几根K线中AMMA的斜率方向，在新趋势出现时开仓并平掉反向持仓。

## 工作原理

1. 在所选时间框架上计算具有可调周期的 `ModifiedMovingAverage`。
2. 在每根完成的K线上比较最近三个AMMA值。
3. 如果指标值呈上升序列且最新值高于前一值，则开多单并关闭所有空单。
4. 如果指标值呈下降序列且最新值低于前一值，则开空单并关闭所有多单。

## 参数

- `CandleType` – 用于计算的K线类型。
- `MaPeriod` – AMMA的周期。
- `AllowLongEntry` – 允许开多。
- `AllowShortEntry` – 允许开空。
- `AllowLongExit` – 允许平多。
- `AllowShortExit` – 允许平空。

## 说明

策略仅在完成的K线上运行，并使用内置的 `BuyMarket` 和 `SellMarket` 方法执行订单。风险管理可以通过`Strategy`的标准属性在外部添加。
