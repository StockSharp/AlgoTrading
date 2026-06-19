# RMACD反转策略

## 概述
该策略使用MACD指标产生反转信号。通过四种模式确定入场条件：

1. **Breakdown** – 当MACD柱状图下穿零轴时做多，上穿零轴时做空。
2. **MacdTwist** – 通过比较最近两个柱状图值检测MACD方向改变。
3. **SignalTwist** – 监控信号线方向的变化。
4. **MacdDisposition** – 当MACD柱状图与信号线交叉时入场。

策略始终使用市价单，在出现反向信号时反转持仓。

## 参数
- **Fast Length** – MACD中快速EMA的周期。
- **Slow Length** – MACD中慢速EMA的周期。
- **Signal Length** – 信号线的平滑周期。
- **Candle Type** – 计算所使用的K线周期。
- **Mode** – 上述四种入场算法的选择。

## 注意
- 仅在K线收盘后评估信号。
- 策略在内部保存之前的MACD值，不请求历史数据。
- 不设置固定止损或止盈，仓位只在反向信号时平仓。
