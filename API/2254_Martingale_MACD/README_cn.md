# Martingale MACD 策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 框架中重现 MQL "MartGreg_1" 智能交易系统。它使用两个移动平均线收敛/发散 (MACD) 指标来寻找反转，并采用马丁格尔方法管理仓位规模。

## 工作原理

- 第一个 MACD 监控最近三根已完成 K 线，寻找局部峰谷。
- 第二个 MACD 比较最近两根 K 线的数值以判断动量方向。
- 当第一个 MACD 形成谷值且第二个 MACD 下降时开多单。
- 当第一个 MACD 形成峰值且第二个 MACD 上升时开空单。
- 每次亏损后，下单数量按照马丁格尔规则加倍，直到达到设定的上限。
- 止损和止盈以绝对价格点数设置。

## 参数

- `Shape` – 账户余额除数，用于计算初始手数。
- `Doubling Count` – 允许连续加倍的最大次数。
- `Stop Loss` – 止损点数。
- `Take Profit` – 止盈点数。
- `MACD1 Fast/Slow` – 第一个 MACD 的周期。
- `MACD2 Fast/Slow` – 第二个 MACD 的周期。
- `Candle Type` – 分析使用的时间框。

