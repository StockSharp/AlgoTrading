# Color Schaff TRIX Trend Cycle 策略

该策略实现了基于 TRIX 和 MACD 的 **Schaff Trend Cycle** 振荡指标。当振荡器跨越预设水平时，识别趋势的周期性变化并产生交易信号。

## 工作原理

1. 计算两个不同周期的 TRIX 振荡器以构建 MACD 序列。
2. MACD 值经过两次随机指标变换得到 Schaff Trend Cycle (STC)。
3. 当 STC 上穿高位水平时开多头仓位，下穿低位水平时开空头仓位。
4. 当出现反向交叉时平掉现有仓位。

## 参数

- **Fast TRIX** – 快速 TRIX 的周期。
- **Slow TRIX** – 慢速 TRIX 的周期。
- **Cycle** – 随机指标计算周期。
- **High Level / Low Level** – STC 的上下界。
- **Stop Loss % / Take Profit %** – 风险控制参数，以百分比表示。
- **Buy/Sell Open/Close** – 控制是否允许开仓或平仓。

## 说明

策略使用所选时间框架的K线数据（默认4小时）并以市价单执行。启用了带有止损和止盈的保护。所有指标处理均通过高级 API 自动完成。

本策略仅用于学习示例，实际交易前请务必充分回测。
