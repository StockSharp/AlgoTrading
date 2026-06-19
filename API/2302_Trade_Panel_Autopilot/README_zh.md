# Trade Panel Autopilot 策略

## 概述

该策略复现原始 MQL4 “trade panel with autopilot” 专家的核心逻辑。它在多个时间框中统计价格方向，根据市场的主要趋势自动开仓或平仓。

策略在八个时间框（M1、M5、M15、M30、H1、H4、D1、W1）上跟踪最近两根K线，并比较以下价格组成部分：

- 开盘价
- 最高价
- 最低价
- (High + Low) / 2
- 收盘价
- (High + Low + Close) / 3
- (High + Low + Close + Close) / 4

每次比较都会增加**买入**或**卖出**计数。所有时间框的计数合并后转换为百分比。当买入或卖出百分比超过设定阈值时，策略开仓；当相反方向的百分比下降到平仓阈值以下时，策略平仓。

## 参数

- `Autopilot` — 是否启用自动交易。
- `OpenThreshold` — 开仓所需的百分比阈值，默认 85。
- `CloseThreshold` — 平仓所需的百分比阈值，默认 55。
- `LotFixed` — 当 `UseFixedLot` 启用时使用的固定下单量。
- `LotPercent` — 当 `UseFixedLot` 关闭时按账户权益百分比计算的下单量。
- `UseFixedLot` — 在固定下单量与按百分比计算之间切换。
- `UseStopLoss` — 启用后启动仓位保护。

## 交易逻辑

1. 订阅所有时间框的K线数据。
2. 对每根完成的K线计算买入和卖出计数。
3. 汇总所有时间框的计数并计算百分比。
4. 如果 `Autopilot` 关闭，策略仅监控结果。
5. 当没有持仓且买入百分比超过 `OpenThreshold` 时开多单；当卖出百分比超过阈值时开空单。
6. 持有多单时若买入百分比下降到 `CloseThreshold` 以下则平仓；持有空单时使用卖出百分比判断是否平仓。

## 说明

- 策略任意时刻只保持一个仓位。
- 当 `UseStopLoss` 为真时调用 `StartProtection()` 以启用止损管理。
