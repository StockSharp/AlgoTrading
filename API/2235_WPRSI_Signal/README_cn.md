# WPRSI 信号策略

## 概述
该策略复现 MetaTrader 中的 WPRSIsignal 专家。策略结合 Williams 百分比范围 (WPR) 与 相对强弱指数 (RSI) 来产生买入和卖出信号。

## 逻辑
- 当 WPR 从下方穿越 -20 且 RSI 大于 50 时产生 **买入** 信号。只有在之后的 `FilterUp` 根 K 线中 WPR 保持在 -20 之上时该信号才被确认。
- 当 WPR 从上方穿越 -80 且 RSI 小于 50 时产生 **卖出** 信号。只有在之后的 `FilterDown` 根 K 线中 WPR 保持在 -80 之下时该信号才被确认。
- 信号确认后，如果当前没有同方向的仓位则开仓。仓位通过相反信号平仓。

## 参数
- `Period` – WPR 与 RSI 的计算周期。
- `FilterUp` – 确认买入信号所需的柱数。
- `FilterDown` – 确认卖出信号所需的柱数。
- `CandleType` – 用于计算的 K 线周期。

## 用法
将策略附加到任何标的。策略使用 `SubscribeCandles` 和 `Bind` 获取 K 线和指标数值。通过 `BuyMarket` 和 `SellMarket` 下达市价单。策略不包含止损或止盈，仓位通过相反信号平仓。
