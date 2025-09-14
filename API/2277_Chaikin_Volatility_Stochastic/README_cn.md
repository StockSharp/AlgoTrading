# Chaikin Volatility Stochastic Strategy

该策略将随机振荡器应用于Chaikin波动率以捕捉趋势反转。每根K线的高低范围先通过EMA平滑，再用随机指标归一化，最后使用加权移动平均进一步平滑。

当平滑后的振荡器在上升后转向下行时，开多并平掉所有空头；当振荡器在下降后转向上行时，开空并平掉所有多头。

## 参数
- **Candle Type**：订阅的K线周期。
- **EMA Length**：高低范围的平滑周期。
- **Stochastic Length**：随机指标的计算窗口。
- **WMA Length**：振荡器平滑用的加权平均周期。
- **Enable Longs / Enable Shorts**：允许的交易方向。

## 指标
- ExponentialMovingAverage
- Highest 和 Lowest
- WeightedMovingAverage

## 交易规则
- **做多**：振荡器先上升后转为下行。
- **做空**：振荡器先下降后转为上行。
- 信号反转时平掉相反仓位。
