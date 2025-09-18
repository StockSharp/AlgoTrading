# Multi Indicator Optimizer 策略

该策略在 StockSharp 高级 API 上还原 MetaTrader 专家顾问 **MultiIndicatorOptimizer** 的投票机制。五个经典振荡指标在每根已完成的 K 线上给出加权投票，最终汇总成一条综合信号，再根据用户设定的阈值决定是做多、做空还是平仓观望。

## 交易逻辑

1. **MACD 模块**：检查柱状图符号以及主线与信号线的相对位置（均基于上一根收盘 K 线），两种判断结果求平均后乘以 `MacdWeight`。
2. **Awesome Oscillator 模块**：判断指标是否位于零轴上方，同时比较动能相对于前一根 K 线是否增强，平均得分乘以 `AoWeight`。
3. **OsMA 模块**：读取上一根 K 线的 MACD 柱状图符号并乘以 `OsmaWeight`。
4. **Williams %R 模块**：检测是否突破 `WilliamsLowerLevel` 与 `WilliamsUpperLevel` 所定义的超卖/超买区。向上离开下轨给出看涨票，向下跌破上轨给出看跌票，并乘以 `WilliamsWeight`。
5. **Stochastic 模块**：组合两个条件——%K 穿越 `StochasticLowerLevel`/`StochasticUpperLevel`，以及 %K 与 %D 的大小关系。两个子信号平均后乘以 `StochasticWeight`。

综合得分会写入日志的 `Signal` 列，同时保存在 `_lastSignal` 字段。交易引擎按以下规则处理：

- `signal >= EntryThreshold`：如有空头则平仓，并建立/保持多头。
- `signal <= -EntryThreshold`：如有多头则平仓，并建立/保持空头。
- `abs(signal) <= ExitThreshold`：若市场处于中性区间，则平仓观望。

所有判断均基于上一根完成的 K 线，与原始 MQL 版本使用的 `shift = 1/2` 索引保持一致。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 指标计算所用的主时间框架。 | H1 K 线 |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD 模块的 EMA 周期。 | 12 / 26 / 9 |
| `MacdWeight` | MACD 模块的投票权重（可为负以反向）。 | 1 |
| `AoShortPeriod` / `AoLongPeriod` | Awesome Oscillator 的快/慢均线周期。 | 5 / 34 |
| `AoWeight` | Awesome 模块权重。 | 1 |
| `OsmaFastPeriod` / `OsmaSlowPeriod` / `OsmaSignalPeriod` | 构建 OsMA 柱状图的 MACD 参数。 | 12 / 26 / 9 |
| `OsmaWeight` | OsMA 模块权重。 | 1 |
| `WilliamsPeriod` | Williams %R 的回溯长度。 | 14 |
| `WilliamsLowerLevel` / `WilliamsUpperLevel` | 超卖/超买边界（百分比）。 | -80 / -20 |
| `WilliamsWeight` | Williams 模块权重。 | 1 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | 随机指标 %K、%D 及平滑参数。 | 5 / 3 / 3 |
| `StochasticLowerLevel` / `StochasticUpperLevel` | %K 的超卖/超买阈值。 | 20 / 80 |
| `StochasticWeight` | Stochastic 模块权重。 | 1 |
| `EntryThreshold` | 开仓或反向所需的最小绝对得分。 | 0.5 |
| `ExitThreshold` | 中性区间宽度：当 信号的绝对值小于该值时全部平仓。 | 0.1 |

各个权重均允许设置为负值，以关闭或反向某个模块的投票，便于参数优化。

## 说明

- 全部逻辑基于高级 API：使用 `SubscribeCandles` 订阅、指标绑定以及 `BuyMarket`/`SellMarket` 下单助手。
- 仅在蜡烛收盘后更新信号，确保决策来自确认数据。
- 持仓量由 `Strategy.Volume` 决定，若需要止盈止损可以额外调用 `StartProtection` 设置。
- 代码内附有详细的英文注释，方便继续维护与扩展。
