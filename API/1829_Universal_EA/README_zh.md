# Universal EA 策略

该策略由 MQL4 的 "Universal_EA" 转换而来。

策略使用随机指标(Stochastic)寻找入场点。当 %K 线上穿 %D 线并且两者都位于超卖水平下方时开多仓；当 %K 线下穿 %D 线并且两者都位于超买水平上方时开空仓。信号只在已完成的K线上检查，交易以市价单执行。

## 参数
- **%K Period** – 计算 %K 的基础周期。
- **%D Period** – %D 线的平滑周期。
- **Slowing** – 对 %K 线的额外平滑。
- **Oversold** – 判定超卖的阈值。
- **Overbought** – 判定超买的阈值。
- **Candle Type** – 用于分析的K线类型或周期。
