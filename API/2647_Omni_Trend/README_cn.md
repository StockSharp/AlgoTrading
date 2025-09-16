# Omni Trend 策略

## 概述

Omni Trend 策略是 MetaTrader 专家顾问 "Exp_Omni_Trend" 的 StockSharp 版本。策略由移动均线与基于 ATR 的波动通道组成，价格突破前一根柱子的保护带时即视为趋势反转，系统会平掉相反方向的持仓并在新的方向上建立仓位。实现保留了原始顾问的所有关键选项，包括信号执行延迟 (`SignalBar`) 以及针对多空的独立开仓/平仓开关。

策略订阅选定的 K 线序列，仅对已完成的 K 线进行计算：

- 移动均线 (`MaType`, `MaLength`, `AppliedPrice`) 给出趋势中心；
- ATR (`AtrLength`) 与 `VolatilityFactor`、`MoneyRisk` 一起生成自适应上下轨，这些轨道在逻辑上等同于跟踪止损。

当价格的最高价突破上一根柱子的上轨时，趋势切换为多头；最低价跌破下轨时，趋势切换为空头。趋势发生变化时会立即发出与新方向一致的开仓指令，同时不断要求关闭所有与趋势相反的头寸（前提是对应开关启用）。

可选的 `StopLossPoints`、`TakeProfitPoints` 以价格最小变动单位为度量，用于在指标信号之外附加硬性止损/止盈。仓位大小由策略自身的 `Volume` 属性控制，默认值为 `1`。

## 交易流程

1. 根据所选价格字段计算移动均线。
2. 计算 ATR 并生成上下轨道。
3. 若 `HighPrice` 突破上一根柱子的上轨：
   - 趋势切换为向上；
   - 如启用 `EnableSellClose`，立即平掉所有空单；
   - 如启用 `EnableBuyOpen`，在队列中记录一个多单信号，延迟 `SignalBar` 根柱子执行。
4. 若 `LowPrice` 跌破上一根柱子的下轨：
   - 趋势切换为向下；
   - 如启用 `EnableBuyClose`，立即平掉所有多单；
   - 如启用 `EnableSellOpen`，在队列中记录一个空单信号，按照设定延迟执行。
5. 在趋势维持期间，对应方向的平仓信号会持续出现，确保仓位始终顺应当前趋势。
6. 每根完成的 K 线都会触发风险管理检查：当价格触及止损或止盈水平（以最小价格单位表示）时立即平仓，并清除记忆的入场价。

信号通过 FIFO 队列调度。`SignalBar = 0` 时，信号在当前 K 线收盘时执行；大于零时，执行发生在完成延迟的那根 K 线开盘价附近，与原顾问使用上一根柱子信号、下一根柱子执行的模式一致。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 计算所用的 K 线类型/周期。 | 4 小时 |
| `MaLength` | 移动均线周期。 | 13 |
| `MaType` | 移动均线方法：Simple、Exponential、Smoothed、LinearWeighted。 | Exponential |
| `AppliedPrice` | 移动均线使用的价格字段：Close、Open、High、Low、Median、Typical、Weighted。 | Close |
| `AtrLength` | ATR 周期。 | 11 |
| `VolatilityFactor` | ATR 乘数，用于生成原始通道。 | 1.3 |
| `MoneyRisk` | 通道相对均线的位移系数。 | 0.15 |
| `SignalBar` | 信号执行前需要等待的已完成 K 线数量。 | 1 |
| `EnableBuyOpen` | 允许开多。 | true |
| `EnableSellOpen` | 允许开空。 | true |
| `EnableBuyClose` | 允许在趋势转空时平掉多头。 | true |
| `EnableSellClose` | 允许在趋势转多时平掉空头。 | true |
| `StopLossPoints` | 止损距离（价格最小变动单位），0 表示禁用。 | 1000 |
| `TakeProfitPoints` | 止盈距离（价格最小变动单位），0 表示禁用。 | 2000 |
| `Volume` | 策略持仓数量属性。 | 1 |

## 使用建议

- `SignalBar = 1` 能够复刻原顾问的默认行为：信号产生后在下一根 K 线开盘附近成交。设置为 `0` 时表示在当根 K 线收盘执行。
- 使用止损或止盈前请确认标的提供有效的 `PriceStep`。
- 策略会在图表上绘制 K 线、所选移动均线以及自身成交，便于快速验证逻辑。
- 可以通过关闭 `Enable*` 开关让策略只做多或只做空，或由人工接管平仓。
- 策略仅发送市价单（`BuyMarket`/`SellMarket`），与 MQL 版本直接下单的方式一致。

## 文件结构

- `CS/OmniTrendStrategy.cs` — 策略的 C# 实现。
- `README.md`, `README_ru.md`, `README_cn.md` — 英语、俄语、中文说明文档。

根据任务要求，本次未提供 Python 版本。
