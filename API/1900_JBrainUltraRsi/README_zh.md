# JBrainUltraRSI 策略

该示例策略结合相对强弱指数 (RSI) 与 随机振荡指标(Stochastic) 来生成交易信号。
原始的 MetaTrader 专家顾问使用 *JBrainTrendSig1* 与 *UltraRSI* 指标。本改写版本中使用随机振荡指标作为趋势过滤器，RSI 用于给出入场信号。

## 工作原理

1. **指标**
   - **RSI**：比较近期上涨和下跌幅度。当 RSI 向上穿过 50 表示看涨动能，向下穿过 50 表示看跌动能。
   - **随机振荡指标**：比较收盘价与近期区间的位置。%K 与 %D 线的交叉确认趋势方向。
2. **模式**
   - **JBrainSig1Filter** – RSI 产生信号，随机振荡指标确认方向。
   - **UltraRsiFilter** – 随机振荡指标产生信号，RSI 进行过滤。
   - **Composition** – 只有当两个指标方向一致时才开仓。
3. **交易规则**
   - 出现买入信号且没有空头头寸时开多头。
   - 出现卖出信号且没有多头头寸时开空头。
   - 反向信号在允许的情况下关闭已有头寸。

## 参数

| 参数 | 说明 |
|------|------|
| `RsiPeriod` | RSI 计算周期 |
| `StochLength` | 随机指标 %K 周期 |
| `SignalLength` | 随机指标 %D 周期 |
| `Mode` | 指标组合模式 |
| `AllowLongEntry` / `AllowShortEntry` | 是否允许开多/开空 |
| `AllowLongExit` / `AllowShortExit` | 是否允许平多/平空 |
| `CandleType` | 使用的K线周期 |

## 说明

- 策略使用 StockSharp 高级 API，通过 `Bind` / `BindEx` 处理指标。
- 可以通过 `StartProtection()` 方法设置止盈止损保护。
- 若图表区域可用，策略会绘制K线、指标以及自身交易。
