# Exp Leading 策略

该策略基于自定义 **Leading** 指标的交叉系统，该指标源自 John F. Ehlers 的《Cybernetics Analysis for Stock and Futures》。指标包含两条曲线：

1. **NetLead** – 由参数 `Alpha1` 和 `Alpha2` 控制的平滑领先滤波器。
2. **EMA** – 采用固定系数 0.5 的指数移动平均。

策略在选定周期的已完成K线运行。当 NetLead **下穿** EMA 时，预期行情向上反转并开多单；当 NetLead **上穿** EMA 时，开空单。若已有持仓，会在发送反向订单时自动平仓。

## 参数

- `Alpha1` – 领先计算的系数，默认 `0.25`。
- `Alpha2` – 应用于领先结果的平滑因子，默认 `0.33`。
- `CandleType` – 用于计算的K线类型，默认四小时周期。
- `StopLoss` – 绝对价格单位的止损，默认 `1000`。
- `TakeProfit` – 绝对价格单位的止盈，默认 `2000`。

## 交易逻辑

1. 每根完成的K线都会更新 NetLead 和 EMA 数值。
2. 若上一根K线 NetLead 高于 EMA，而最新一根 NetLead 低于 EMA，则发送**买入**市价单。
3. 若上一根 NetLead 低于 EMA，而最新一根高于 EMA，则发送**卖出**市价单。
4. 通过 `StartProtection` 自动应用止损和止盈。

该示例用于演示如何将 MetaTrader 策略迁移到 StockSharp 的高级 API。
