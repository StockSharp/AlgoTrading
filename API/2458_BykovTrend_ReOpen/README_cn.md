# BykovTrend ReOpen 策略

## 概述
BykovTrend ReOpen 策略基于 Williams %R 和平均真实波幅（ATR）来判定趋势。当趋势转为上升时产生买入信号，趋势转为下降时产生卖出信号。进入仓位后，只要趋势持续，策略会在每个设定的价格步长重新加仓。止损和止盈以最近一次开仓价为基准。

## 指标
策略直接使用以下指标计算信号：
- **Williams %R**，周期为 `SSP`。
- **ATR**，固定周期 15。
当 Williams %R 穿越阈值 `-100 + K` 和 `-K`（其中 `K = 33 - Risk`）时认为趋势改变。

## 交易规则
1. 出现看涨信号时关闭空头仓位（若允许）并开多头。
2. 出现看跌信号时关闭多头仓位（若允许）并开空头。
3. 持有仓位期间，只要价格每移动 `Price Step` 单位且未达到 `Max Positions`，便在同方向加仓。
4. 每次开仓都设置距离为 `Stop Loss` 和 `Take Profit` 的止损和止盈。

## 参数
- `Risk` – 风险因子，决定指标阈值。
- `SSP` – Williams %R 周期。
- `Price Step` – 加仓所需的价格距离。
- `Max Positions` – 同方向最大持仓数量。
- `Stop Loss` – 止损距离（价格单位）。
- `Take Profit` – 止盈距离（价格单位）。
- `Enable Long Open` – 允许开多。
- `Enable Short Open` – 允许开空。
- `Enable Long Close` – 允许在反向信号时平多。
- `Enable Short Close` – 允许在反向信号时平空。
- `Candle Type` – 计算所用的时间框架。
