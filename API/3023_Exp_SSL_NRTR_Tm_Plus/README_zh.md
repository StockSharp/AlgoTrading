# Exp SSL NRTR Tm Plus 策略

## 概述

本策略在 StockSharp 框架中复刻 MetaTrader 顾问 "Exp_SSL_NRTR_Tm_Plus"。系统只订阅一个时间框，计算带有可选平滑方式的
SSL NRTR 通道，并根据颜色切换执行交易。通道变为绿色时尝试做多，变为粉色时尝试做空，同时保留原始 EA 中的风险控制、
过滤器和持仓计时器。

## 参数

| 组别 | 参数 | 说明 |
| --- | --- | --- |
| Trading | Money Management | 头寸规模配置：正值表示账户资金比例，负值或 `Lot` 模式表示直接下单手数。 |
| Trading | Margin Mode | 将 Money Management 转换为订单数量的方法。除 `Lot` 外的模式通过账户价值近似计算。 |
| Trading | Allow Long/Short Entries | 是否允许开仓做多/做空。 |
| Trading | Allow Long/Short Exits | 是否允许在反向信号出现时平掉多头/空头。 |
| Risk | Stop Loss | 止损距离（以价格步长计），由策略内部监控。 |
| Risk | Take Profit | 止盈距离（以价格步长计）。 |
| Risk | Slippage | 来源于原始 EA 的信息型参数，策略仍以市价单成交。 |
| Risk | Use Time Exit | 启用持仓计时器，到期后强制平仓。 |
| Risk | Exit Minutes | 持仓时间（分钟）。 |
| Data | Candle Type | 使用的 K 线时间框。 |
| Indicator | Smoothing Method | SSL NRTR 的平滑方法。当前仅支持 StockSharp 内置均线，`Jurx` 与 `Parma` 会退化为 EMA。 |
| Indicator | Length | 平滑周期。 |
| Indicator | Phase | 自适应均线的附加参数（T3、VIDYA、AMA）。 |
| Indicator | Signal Bar | 回溯多少根已完成的 K 线来读取信号。 |

## 交易逻辑

1. 仅处理已完成的 K 线，避免前瞻性信号。
2. 计算 SSL NRTR 通道并判断当前颜色。
3. 颜色切换为绿色 (`0`) 时，可选地平掉空单并在允许情况下开多。
4. 颜色切换为粉色 (`2`) 时，可选地平掉多单并在允许情况下开空。
5. 根据入场价跟踪止损/止盈水平，触发后立即平仓。
6. 若启用计时器，持仓超过 `Exit Minutes` 自动平仓。
7. 通过时间节流机制避免同一根 K 线上重复开仓。

## 资金管理

- `Lot` 模式把 Money Management 视为直接下单手数。
- `FreeMargin` 与 `Balance` 模式近似按照账户资金与当前价格换算手数。
- `LossFreeMargin` 与 `LossBalance` 模式根据止损距离估算允许的风险敞口。
- Money Management 设为负值时始终使用其绝对值作为固定手数。

## 备注

- 仅实现了 StockSharp 支持的平滑算法，`Jurx` 与 `Parma` 自动回退为 EMA，详情见源码注释。
- 为保持平台无关性，止损与止盈由策略内部管理，不会发送真实保护单。
- Slippage 参数仅用于记录，所有下单都是市价单。
- 默认在图表上绘制 K 线和策略自身成交。 
