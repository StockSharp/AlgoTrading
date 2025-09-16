# Ichimoku Oscillator 策略

**Ichimoku Oscillator** 策略基于 Ichimoku 指标构建的振荡器。该振荡器等于滞后线与 Senkou Span B 的差值，再减去 Tenkan-sen 与 Kijun-sen 的差值，然后使用 Jurik 移动平均进行平滑。

当平滑后的振荡器改变方向并突破前一个值时，策略尝试进入交易以捕捉新趋势。

## 策略逻辑
- **做多**：振荡器上升且当前值向上穿越前一个值。开多前会先平掉空头。
- **做空**：振荡器下降且当前值向下穿越前一个值。开空前会先平掉多头。
- 可选的百分比止损和止盈用于风险控制。

## 参数
- **Tenkan Period** – Ichimoku 指标中 Tenkan-sen 的周期。
- **Kijun Period** – Ichimoku 指标中 Kijun-sen 的周期。
- **Senkou Span B Period** – Ichimoku 指标中 Senkou Span B 的周期。
- **Smoothing Period** – Jurik 移动平均的平滑周期。
- **Candle Type** – 计算所用的时间框架。
- **Stop Loss %** – 以百分比表示的止损。
- **Enable Stop Loss** – 是否启用止损。
- **Take Profit %** – 以百分比表示的止盈。

## 指标
- Ichimoku
- Jurik Moving Average

## 注意
本策略仅用于教育目的，实际交易前请先在历史数据上进行测试。
