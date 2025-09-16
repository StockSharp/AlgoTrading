# NRTR ATR Stop 策略

## 概述
NRTR ATR Stop 策略在 StockSharp 的高级 API 上复刻了 MetaTrader 专家顾问 `Exp_NRTR_ATR_STOP` 的运行逻辑。它追踪基于平均真实波幅（ATR）的 NRTR（Non-Repainting Trailing Reverse）动态止损线。当价格突破对侧止损线时，趋势立即翻转，策略会同时平掉旧方向仓位并在新方向开仓。

## 指标逻辑
* 订阅的 K 线数据用于计算单一 **ATR**（长度由 `AtrPeriod` 决定），ATR 与 `Coefficient` 的乘积决定了价格与止损线之间的距离。
* 策略维护两条动态止损：
  * `upper stop` 在多头趋势中位于价格下方，为多头仓位提供跟踪保护。
  * `lower stop` 在空头趋势中位于价格上方，为空头仓位提供跟踪保护。
* 当收盘价突破对侧止损线时，趋势立即翻转。新的止损线以上一根 K 线的极值减/加 ATR 距离初始化。
* 原始 EA 通过读取指标缓存中 `SignalBar` 根历史柱的数据来延迟执行。策略内部使用一个队列重现这一行为：每根完成的 K 线都会将信号写入队列，只有当队列长度大于 `SignalBar` 时才真正触发交易。

## 交易规则
1. **买入信号** —— 计算得到的趋势由空头或中性转为多头。策略可选地一次性买入（`BuyMarket`）平掉所有空头，并在同一笔市场单中加入 `Volume` 指定的新多头仓位。
2. **卖出信号** —— 趋势由多头或中性转为空头。策略可选地一次性卖出（`SellMarket`）平掉所有多头，并附带 `Volume` 指定的新空头仓位。
3. `EnableLongEntry`、`EnableShortEntry`、`EnableLongExit` 与 `EnableShortExit` 属性用于精确控制在信号出现时应执行的操作。
4. 仅在 K 线收盘且策略处于在线并允许交易的状态下才处理信号。

## 参数
| 名称 | 说明 |
| --- | --- |
| `AtrPeriod` | 计算 ATR 所使用的 K 线数量。 |
| `Coefficient` | 构建跟踪止损时乘在 ATR 上的系数。 |
| `SignalBar` | 在执行保存的信号之前需要等待的完整 K 线数量，设为 `0` 表示立即执行。 |
| `CandleType` | 输入 K 线的时间框架。 |
| `EnableLongEntry` | 允许在买入信号时开多。 |
| `EnableShortEntry` | 允许在卖出信号时开空。 |
| `EnableLongExit` | 允许在卖出信号时平掉现有多头。 |
| `EnableShortExit` | 允许在买入信号时平掉现有空头。 |

## 注意事项
* 策略仅基于收盘完成的 K 线进行计算，不处理盘中逐笔数据。
* 所有交易通过 `BuyMarket` / `SellMarket` 市价指令完成，方便在一笔订单中同时平仓并反向开仓。
* 在实盘或回测前请确保策略的 `Volume` 属性被设置为正数。
