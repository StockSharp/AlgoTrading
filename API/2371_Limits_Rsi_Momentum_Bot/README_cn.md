# Limits RSI Momentum Bot 策略

## 概要
该策略基于相对强弱指数（RSI）和动量（Momentum）指标，通过在K线开盘价附近放置限价单来实现折价买入和溢价卖出。

## 交易规则
- 仅在指定的时间段内运行。
- 每根完成的K线都会计算 RSI 和 Momentum 值。
- 当 RSI 和 Momentum 同时低于买入阈值时，在开盘价下方放置买入限价单。
- 当 RSI 和 Momentum 同时高于卖出阈值时，在开盘价上方放置卖出限价单。
- 开仓后会取消相反方向的挂单。
- 使用 `StartProtection` 自动管理止损和止盈。

## 参数
- `Volume` – 下单数量。
- `LimitOrderDistance` – 距离开盘价的价格步长，用于放置挂单。
- `TakeProfit` – 盈利目标（价格步长）。
- `StopLoss` – 亏损限制（价格步长）。
- `RsiPeriod` – RSI 计算周期。
- `RsiBuyRestrict` / `RsiSellRestrict` – 允许做多或做空的 RSI 阈值。
- `MomentumPeriod` – Momentum 计算周期。
- `MomentumBuyRestrict` / `MomentumSellRestrict` – 允许做多或做空的 Momentum 阈值。
- `StartTime` / `EndTime` – 交易时间区间。
- `CandleType` – 用于计算指标的K线周期。

## 备注
该策略由 MQL4 脚本 "The Limits Bot with RSI & Momentum" 转换，并使用 StockSharp 的高级 API。
