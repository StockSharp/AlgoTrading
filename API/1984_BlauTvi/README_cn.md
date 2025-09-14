# Blau TVI 策略

该策略将 MQL5 智能交易系统 `Exp_BlauTVI` 转换为 StockSharp 的高级策略，实现 **Blau True Volume Index (TVI)** 指标来捕捉成交量流向的反转。

## 思路

True Volume Index 将成交量分为上行与下行两部分，并通过三次指数移动平均进行平滑处理。最终数值在 -100 到 +100 之间变化，显示买方或卖方的主导力量。当指标在下行后向上转折时做多，并在上行后向下转折时做空。反向持仓会被平仓。

## 参数

- `Length1` – 第一次平滑的周期。
- `Length2` – 第二次平滑的周期。
- `Length3` – 对 TVI 的最终平滑周期。
- `CandleType` – 计算所用的蜡烛类型（默认为 4 小时时间框架）。
- `Allow Buy Open` – 允许开多头仓位。
- `Allow Sell Open` – 允许开空头仓位。
- `Allow Buy Close` – 出现卖出信号时允许平掉多头。
- `Allow Sell Close` – 出现买入信号时允许平掉空头。
- `Enable Stop Loss` – 启用以点数表示的止损。
- `Stop Loss` – 止损点数。
- `Enable Take Profit` – 启用以点数表示的止盈。
- `Take Profit` – 止盈点数。
- `Volume` – 下单手数。

## 信号

1. **买入** – 前一个 TVI 小于两根之前的数值且当前 TVI 大于前一个值，同时可选择平掉空头。
2. **卖出** – 前一个 TVI 大于两根之前的数值且当前 TVI 小于前一个值，同时可选择平掉多头。

仅处理已完成的蜡烛，计算基于蜡烛的 tick 成交量。止损与止盈为可选项，以价格点数表示。

## 说明

策略使用高级 API：订阅蜡烛、通过 `ExponentialMovingAverage` 计算指标，并通过 `BuyMarket` 与 `SellMarket` 方法管理仓位。图表会显示 TVI 指标和策略执行的交易。
