# Bollinger Volatility Breakout
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

策略使用已完成的5分钟K线，等待指标形成并允许交易。所有滚动统计都包含信号K线。`C`为收盘价，`M = SMA(C, 20)`，`D = 总体标准差(C, 20)`，`U = M + 2 × D`，`L = M − 2 × D`，`A = ATR(14)`，采用Wilder平滑真实波幅。

高波动条件为 `A >= SMA(A, 14) + 2 × 总体标准差(A, 14)`。价格水平对应布林带的上轨、中轨和下轨。

空仓时，高波动条件成立且 `C >= U` 则买入，成立且 `C <= L` 则卖出。开仓数量为 `Volume`。只有冷却期结束且没有待成交订单时才能再次开仓。 开仓要求 `A > 0`。

`k = StopLossMultiplier`. 开仓时固定信号K线的收盘价及ATR：多头止损为 `C_entry − k × A_entry`，空头止损为 `C_entry + k × A_entry`。每根K线完成后，如果收盘价达到固定止损，多头满足 `C <= M`，空头满足 `C >= M`，或高波动条件失效，则用市价单平掉全部实际持仓。

每次下单后的接下来84根完整K线禁止新开仓；冷却期间仍检查全部退出条件。止损在K线收盘时检查，市价单按可成交价格执行。持仓期间止损距离保持固定。不设置止盈或移动止损。

## 默认参数

```text
BollingerPeriod = 20
BollingerDeviation = 2
AtrPeriod = 14
AtrDeviationMultiplier = 2
StopLossMultiplier = 2
CooldownBars = 84
CandleType = TimeSpan.FromMinutes(5).TimeFrame()
```
