# Exp Digital MACD 策略

## 概述
Exp Digital MACD 策略在 StockSharp 框架中复刻了 MetaTrader 5 专家顾问 “Exp_Digital_MACD” 的逻辑。策略订阅指定周期的收盘 K 线，并根据 MACD 类振荡器的相对位置与斜率生成信号。源代码中的四种工作模式在此得到保留：

1. **Breakdown** – 关注振荡器的零轴穿越。
2. **MACD Twist** – 捕捉 MACD 线出现拐点的瞬间。
3. **Signal Twist** – 使用信号线自身的拐点作为确认。
4. **MACD Disposition** – 当 MACD 柱线突破信号线时触发操作。

由于 StockSharp 未内置专有的 “Digital MACD” 滤波器，本策略使用标准的 `MovingAverageConvergenceDivergenceSignal` 指标，并将快慢 EMA 设为 12/26、信号 EMA 设为 5，以贴近原始脚本的设定。策略仅处理已完成的 K 线，并通过私有字段保存少量历史值，以模拟 MQL 版本中 `SignalBar = 1` 的行为。

## 参数
- **Mode** – 选择上述四种交易模式之一，默认值：`MacdTwist`。
- **FastPeriod** – MACD 使用的快 EMA 长度，默认值：`12`。
- **SlowPeriod** – MACD 使用的慢 EMA 长度，默认值：`26`。
- **SignalPeriod** – 信号线的 EMA 长度，默认值：`5`，对应原始专家顾问的设置。
- **CandleType** – 订阅 K 线的周期，默认值：`4 小时`。
- **OrderVolume** – 每次市价单的下单量（手数或合约数）。
- **StopLossPoints / TakeProfitPoints** – 以最小报价步长表示的止损与止盈偏移量。当标的提供有效的 `Step` 值时会自动换算成价格；设为 0 可关闭保护。
- **EnableLongEntry / EnableShortEntry** – 控制是否允许开多或开空。
- **EnableLongExit / EnableShortExit** – 控制策略是否可以平掉现有的多头或空头仓位。

## 交易逻辑
策略在每根 K 线收盘时执行以下判断：

- **Breakdown**：若两根 K 线之前的 MACD 大于零，则可选择平空；若随后一根 K 线回落至零轴以下，则尝试开多。若两根之前的 MACD 小于零，则平多并在下一根上穿零轴时考虑开空，复制了原策略的逆向零轴逻辑。
- **MACD Twist**：跟踪连续三个 MACD 数值。当形成局部低点（value[2] > value[1] 且 value[0] > value[1]）时产生多头信号；局部高点产生空头信号；反向拐点用于离场。
- **Signal Twist**：对信号线执行相同的拐点检测。
- **MACD Disposition**：同时比较 MACD 与信号线。当 MACD 先前高于信号线而下一次读数回落至其下方时，触发多头入场并平空；反向情况触发做空并平多。

所有开仓指令使用 `OrderVolume + |当前仓位|` 的数量，这样在翻转时可以一次性平掉原有仓位并建立新方向。离场信号只对现有仓位发送平仓市价单。

## 风险管理
策略启动后即调用 `StartProtection`。当 `StopLossPoints` 或 `TakeProfitPoints` 大于零且品种具有有效的 `Step` 时，会自动按绝对价格设置止损/止盈；保持为零则不启用自动保护。

## 实现说明
- 仅评估最新完成的 K 线，对应 MQL 版本中的 `SignalBar = 1`。
- StockSharp 中的 MACD 与专有的 Digital MACD 存在差异，可通过调整 EMA 长度来进一步拟合原策略。
- C# 代码中的所有注释均按照要求使用英文撰写。

## 使用步骤
1. 将策略绑定到目标账户与需要的交易品种，并确保能够获取所需周期的 K 线数据。
2. 根据标的特性调整各项参数。
3. 启动策略后，它会自动订阅 K 线、处理 MACD 数值，并依据选定模式下单。
4. 可通过日志或可选的图表输出来跟踪指标与仓位变化。
