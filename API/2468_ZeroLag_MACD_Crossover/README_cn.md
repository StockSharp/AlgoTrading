# 零滞后 MACD 交叉策略

该策略复刻自 MetaTrader 5 的 **ZeroLagEA-AIP** 算法。它使用基于两条零滞后指数移动平均线的零滞后 MACD。当当前 MACD 值高于前一根柱时开空仓，低于前一根柱时开多仓。如果在持仓期间出现反向信号，当前仓位将被平仓，下一根柱再开新仓。

## 逻辑

1. 计算两个可配置周期的 ZLEMA。
2. 两者差值乘以 10 得到零滞后 MACD。
3. 仅在 MACD 在相邻两根柱之间改变方向时触发交易（可选）。
4. 仅在设定的开始和结束时间内交易，超出时间或在指定的星期和小时强制平仓。

## 参数

- **Volume** – 订单数量。
- **Fast EMA** – 快速 ZLEMA 周期。
- **Slow EMA** – 慢速 ZLEMA 周期。
- **Use Fresh Signal** – 启用后仅在 MACD 方向变化时交易。
- **Start Hour / End Hour** – 以 UTC 表示的交易时间窗口。
- **Kill Day / Kill Hour** – 指定的星期和小时用于强制平仓。
- **Candle Type** – 用于计算的K线类型。

## 说明

策略使用 StockSharp 高级 API，通过 `SubscribeCandles` 和 `Bind` 获取指标数值，并使用市价单平仓。
