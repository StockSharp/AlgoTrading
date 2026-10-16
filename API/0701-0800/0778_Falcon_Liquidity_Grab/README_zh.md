# Falcon Liquidity Grab 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略在主要市场时段内交易流动性抓取，使用简单移动平均线确定趋势。当价格突破近期摆动高低点后沿趋势反转时入场。每笔交易都使用以 tick 为单位的固定止损和止盈。

## 细节

- **入场条件**：
  - **做多**：`Low < 周期最低` && `Close > SMA` && `会话过滤`
  - **做空**：`High > 周期最高` && `Close < SMA` && `会话过滤`
- **离场条件**：固定止损和止盈。
- **类型**：反转
- **指标**：SMA、Highest、Lowest
- **时间框架**：15 分钟（默认）
- **止损**：`StopLossPoints` tick，`TakeProfitMultiplier`× 止损距离

**精确规则与默认值：**`SwingPeriod=20`、`TrendLength=50`、`StopLossPoints=50`、`TakeProfitMultiplier=2`、`UseSession=true`、`SessionStart=07:00`、`SessionEnd=21:00`（UTC）、`CandleType=15分钟`。信号K线完成后，将其高低点与**此前**N根已完成K线的极值比较，不包括当前信号K线。Low严格低于此前最低价且Close高于SMA时做多；High严格高于此前最高价且Close低于SMA时做空。SMA包括当前信号K线。只有空仓时且在UTC时间窗口内开仓（包含起点、不包含终点，支持跨午夜）。基于Level1买卖报价的原生市价保护可在K线之间触发：止损距离=`StopLossPoints × Security.PriceStep`，止盈距离=`TakeProfitMultiplier × 止损距离`。不会因相反收盘信号直接平仓。
