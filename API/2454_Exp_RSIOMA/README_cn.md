# Exp RSIOMA 策略
[English](README.md) | [Русский](README_ru.md)

Exp RSIOMA 策略使用 RSI 移动平均线 (RSIOMA) 指标来捕捉趋势反转与突破。RSI 数值通过额外的移动平均线进行平滑，形成信号线和柱状图。策略支持四种模式：

1. **Breakdown** – 当 RSI 穿越设定的高/低阈值时交易。
2. **HistTwist** – 当柱状图方向发生变化时交易。
3. **SignalTwist** – 当信号线方向发生变化时交易。
4. **HistDisposition** – 当柱状图与信号线交叉时交易。

多头与空头的开仓和平仓可以独立控制。

## 细节

- **入场条件**：取决于 `Mode`
- **多/空**：双向
- **退出条件**：相反信号
- **止损**：无
- **默认值**：
  - `CandleType` = 4 小时
  - `RsiPeriod` = 14
  - `SignalPeriod` = 21
  - `HighLevel` = 20
  - `LowLevel` = -20
- **过滤条件**：
  - 类别: 趋势
  - 方向: 双向
  - 指标: RSI
  - 止损: 无
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等
