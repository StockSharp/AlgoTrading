# MACD AO Pattern 策略

## 概述
该策略是 FORTRADER `MACD.mq5` 智能交易程序的 StockSharp 版本。它实现了所谓的“AOP”形态：当 MACD 指标远离零轴后回抽形成钩形时，系统跟随预期的反转方向开仓，并立即设置以点数表示的固定止损和止盈。

## 策略逻辑
### 数据准备
- 使用 `CandleType` 参数指定的 K 线序列（默认 5 分钟）。
- 采用可配置快线、慢线和信号线周期的标准 MACD 指标（默认 12/26/9）。
- 保存最近三个已完成 K 线的 MACD 主线值，以复现 MQL 中 `iMACD(...,1..3)` 的索引访问。

### 做空（看跌钩形）
1. **预备阶段**：当最近一根完成的 K 线 MACD 主线低于 `BearishExtremeLevel`（默认 −0.0015）时，启动做空观察模式。
2. **回抽到中性**：MACD 升回 `BearishNeutralLevel`（默认 −0.0005）以上，进入钩形确认阶段。
3. **钩形确认**：最近三个 MACD 值需形成局部高点（`macd₁ < macd₂ > macd₃`），同时最新值仍低于中性阈值而再前一根仍高于阈值，确保动能衰减。
4. **入场**：若当前没有多头仓位（`Position <= 0`），按 `OrderVolume` 成交量市价卖出。随后根据 `StopLossPips` 和 `TakeProfitPips`（通过 `GetPipSize` 换算成价格）计算止损与止盈。
5. 若 MACD 转为正值，立即取消做空状态机，等待下一次深度负值伸展。

### 做多（看涨钩形）
1. **预备阶段**：当 MACD 突破 `BullishExtremeLevel`（默认 +0.0015）时，激活做多观察。
2. **立即取消**：若 MACD 跌破零轴，则放弃该多头机会，与原始 EA 保持一致。
3. **回抽到中性**：MACD 回落到 `BullishNeutralLevel`（默认 +0.0005）以下，进入钩形确认。
4. **钩形确认**：最近三个 MACD 值需形成局部低点（`macd₁ > macd₂ < macd₃`），并满足中性阈值条件。
5. **入场**：若当前没有空头敞口（`Position >= 0`），按 `OrderVolume` 市价买入，并设置对称的止损/止盈。

### 风险管理
- `_stopPrice` 与 `_takePrice` 始终跟踪保护价位，每根完成 K 线都会利用最高价/最低价检查是否触发，模拟原 EA 在服务器端执行的效果。
- 点值通过 `Security.PriceStep` 转换为绝对价格。对于 3 位或 5 位报价的外汇品种，乘以 10 以匹配 MQL 对“Fractional pip”的处理。
- 当仓位因保护价位出场后立即清除这些价位，并等待下一轮状态机流程。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 策略使用的 K 线数据类型。 | 5 分钟 |
| `OrderVolume` | 每次市价单的下单量。 | 0.1 |
| `TakeProfitPips` | 止盈距离（点）。已启用优化。 | 60 |
| `StopLossPips` | 止损距离（点）。已启用优化。 | 70 |
| `MacdFastPeriod` | MACD 快速 EMA 长度。 | 12 |
| `MacdSlowPeriod` | MACD 慢速 EMA 长度。 | 26 |
| `MacdSignalPeriod` | MACD 信号线 EMA 长度。 | 9 |
| `BearishExtremeLevel` | 触发做空观察的 MACD 负阈值。 | −0.0015 |
| `BearishNeutralLevel` | 验证看跌钩形的 MACD 负阈值。 | −0.0005 |
| `BullishExtremeLevel` | 触发做多观察的 MACD 正阈值。 | +0.0015 |
| `BullishNeutralLevel` | 验证看涨钩形的 MACD 正阈值。 | +0.0005 |

## 其他说明
- 策略仅在每根完成的 K 线上执行一次计算，对应原始 MQL 中的 `PrevBars` 检查。
- 仅采用固定止损和止盈，不包含追踪机制或重复进场，直到状态机重新激活。
- 原 EA 面向套保账户，本版本通过检查 `Position` 确保在净持仓模式下运行。
- 按要求未提供 Python 版本。
