# Macd Pattern Trader v02（StockSharp 版本）

本策略基于 MetaTrader 专家顾问 **MacdPatternTraderv02.mq4**（目录 `MQL/8194`）移植到 StockSharp 高级 API。它保留了原有的 MACD 形态识别以及分批平仓逻辑，同时提供结构化参数以便优化。

## 核心思路

1. 使用 `FastEmaPeriod` 与 `SlowEmaPeriod` 计算 MACD 主线，信号线长度固定为 1 根K线，与 MQL 程序保持一致。
2. 仅处理已完成的K线。当 MACD 在零轴附近形成特定的四段序列时，分别触发做空或做多的准备状态：
   - **空头形态**：MACD 先处于正值阶段，随后回落到 `MinThreshold` 以上的负值，再次转折向下。
   - **多头形态**：MACD 先处于负值阶段，随后反弹到 `MaxThreshold` 以下的正值，再次转折向上。
3. 形态确认后按 `TradeVolume` 下达市价单。
4. 止损位放在最近 `StopLossBars` 根完成K线的极值之外，并额外加上 `OffsetPoints` 个最小报价点。
5. 止盈位通过连续扫描长度为 `TakeProfitBars` 的窗口来寻找新的极值，直到不再出现更极端的价格。
6. 分批止盈模块与原程序一致：利润达到 5 个点后，如果上一根K线收盘价突破 `Ema2Period` 均线，则减仓三分之一；若上一根K线触及 `(SMA + EMA3) / 2`，再减仓剩余的一半。

## 参数

| 参数 | 说明 |
|------|------|
| `StopLossBars` | 计算止损极值所使用的已完成K线数量。 |
| `TakeProfitBars` | 顺序搜索止盈目标时的窗口长度。 |
| `OffsetPoints` | 在止损极值基础上附加的点数。 |
| `FastEmaPeriod` | MACD 主线的快速 EMA 周期。 |
| `SlowEmaPeriod` | MACD 主线的慢速 EMA 周期。 |
| `MaxThreshold` | 结束空头准备状态的正向阈值。 |
| `MinThreshold` | 结束多头准备状态的负向阈值。 |
| `Ema1Period` | 原策略资金管理模块中的第一条 EMA（保留用于兼容性）。 |
| `Ema2Period` | 用于判定第一次分批止盈的 EMA 周期。 |
| `SmaPeriod` | 与 `Ema3Period` 共同计算第二次分批止盈均线的 SMA 周期。 |
| `Ema3Period` | 与 SMA 配对的慢速 EMA 周期。 |
| `TradeVolume` | 下单的合约数量（手数）。 |
| `CandleType` | 供指标使用的K线数据类型。 |

## 交易流程

- **做空**：当 MACD 序列 `(prev3, prev2, prev1, current)` 满足原始条件（`macdPrev1 < macdPrev3`、`macdPrev1 > macdPrev2`、`current < prev1`、`current < 0` 且通过幅度检查）时触发。如果当前持有多头，会先平掉多头再开空。
- **做多**：逻辑对称（`current > 0` 等条件），若持有空头则先平仓。
- **止损/止盈**：进场后立即计算，不会在持仓过程中重新评估。
- **分批平仓**：盈利达到 5 个点后，若上一根K线收盘价站上 `EMA2`，减仓三分之一；若上一根K线触及 `(SMA + EMA3) / 2`，再减仓剩余的一半。
- **强制退出**：价格触及止损或止盈时立即全平仓位，并重置内部状态。

## 补充说明

- 点值优先使用 `Security.PriceStep`，如无该信息，则根据品种的小数位推导；若仍无法获得，则使用默认值 `0.0001`。
- 策略会缓存最多 1024 根完成K线，以模拟 MQL 中的 `iHighest`、`iLowest` 与原版 `TakeProfit()` 的分段搜索。
- 代码中的注释全部使用英文，满足仓库的统一要求。
- 按任务要求未创建 Python 版本及其目录。
