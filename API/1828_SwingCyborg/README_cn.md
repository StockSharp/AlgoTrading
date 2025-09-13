# Swing Cyborg 策略

## 概述
Swing Cyborg 是一种辅助型策略，根据交易者对中长期趋势的主观判断来执行交易。用户设置预期的趋势方向以及该趋势有效的时间段，策略使用 RSI 指标确认入场并通过固定的目标管理退出。

## 参数
- `Volume` – 下单手数。
- `TrendPrediction` – 预期趋势方向（Uptrend 或 Downtrend）。
- `TrendTimeframe` – 用于计算 RSI 和交易的时间周期（M30、H1 或 H4）。
- `TrendStart` – 趋势开始时间。
- `TrendEnd` – 趋势结束时间。
- `Aggressiveness` – 资金管理等级：
  - 低：止盈 300 点，止损 200 点。
  - 中：止盈 500 点，止损 250 点。
  - 高：止盈 600 点，止损 300 点。

## 交易逻辑
1. 等待所选周期形成新的蜡烛。
2. 仅在当前时间位于 `TrendStart` 和 `TrendEnd` 之间时交易。
3. 计算 RSI(14)。
4. 若没有持仓：
   - `TrendPrediction` 为 Uptrend 且 RSI ≤ 65 时买入。
   - `TrendPrediction` 为 Downtrend 且 RSI ≥ 35 时卖出。
5. 使用 `StartProtection` 在达到预设盈利或亏损点数时自动平仓。

策略仅在蜡烛收盘后作出决策，并且在持有仓位时不会开立新的仓位。
