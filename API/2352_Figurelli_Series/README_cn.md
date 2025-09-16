# Figurelli Series 策略

## 概述
该策略将 MetaTrader5 的 "Exp_FigurelliSeries" 专家顾问转换为 StockSharp。它使用 Figurelli Series 指标，该指标统计当前收盘价上方和下方的移动平均线数量之差。策略在用户设定的开始时间触发一次交易，并在停止时间关闭所有持仓。

## 指标
Figurelli Series 指标从 *Start Period* 开始生成一系列指数移动平均线，每条平均线的周期通过 *Step* 递增，共生成 *Total* 条线。每个柱，指标计算有多少平均线在收盘价上方以及多少在下方。指标值为 `bids - asks`，其中 `bids` 是低于价格的平均线数量，`asks` 是高于价格的平均线数量。

## 交易规则
- 在 `Start Hour:Start Minute` 时刻：
  - 当指标值为正且无多头时买入。
  - 当指标值为负且无空头时卖出。
- 到达或超过 `Stop Hour:Stop Minute` 时刻，关闭所有头寸。
- 仅使用选定 `CandleType` 的已完成蜡烛。

## 参数
- `StartPeriod` – 移动平均线初始周期。
- `Step` – 每条平均线的周期增量。
- `Total` – 移动平均线数量。
- `StartHour` / `StartMinute` – 允许入场的时间。
- `StopHour` / `StopMinute` – 平仓时间。
- `CandleType` – 用于计算的蜡烛类型。
