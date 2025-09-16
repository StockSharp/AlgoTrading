# Starter 策略

**Starter 策略** 是对 MetaTrader 5 专家顾问“Starter (barabashkakvn's edition)”的移植。系统等待商品通道指数（CCI）从极端超卖或超买区间反弹，并结合长周期移动平均线的斜率来确认突破。当动量与趋势过滤器方向一致时，策略按照账户权益的固定风险百分比开仓，仅持有一笔市场头寸。初始止损和可选的跟踪止损完全复刻原始 EA 的资金管理规则。

## 交易逻辑

- **趋势过滤**：可配置的移动平均线（MA）必须以超过 `MaDelta` 的斜率上升才能允许做多，以小于 `-MaDelta` 的斜率下降才能允许做空。支持与 MQL 版本相同的平滑方式（简单、指数、平滑、线性加权）。
- **CCI 确认**：CCI 仅在收盘后评估，当其从下方重新站上 `-CciLevel` 时触发做多信号，从上方跌破 `CciLevel` 时触发做空信号。
- **单一持仓模型**：策略一次只持有一笔仓位。只有在现有仓位平仓后才会接受新的信号，与原专家通过符号和 magic number 限制持仓的逻辑一致。

### 入场规则

1. 等待当前 K 线收盘。
2. 根据设定的偏移量获取移动平均线的当前值与前一值。
3. 计算 CCI 的当前值与前一值。
4. **做多条件**：
   - MA 斜率大于 `MaDelta`（当前 MA 减去前一 MA）。
   - 当前 CCI 大于前一 CCI。
   - CCI 从下方穿越 `-CciLevel`（上一根低于阈值，本根高于阈值）。
5. **做空条件**：
   - MA 斜率小于 `-MaDelta`。
   - 当前 CCI 小于前一 CCI。
   - CCI 从上方跌破 `CciLevel`（上一根高于阈值，本根低于阈值）。

### 离场规则

- **初始止损**：当 `StopLossPips > 0` 时，将成交价减去（或加上）`StopLossPips * PriceStep` 作为初始止损位。
- **跟踪止损**：当 `TrailingStopPips` 和 `TrailingStepPips` 都为正时，价格每改善至少 `TrailingStepPips` 个点，止损就推进到最新价减去（或加上）`TrailingStopPips`。
- **手动平仓**：如果在单根 K 线内触及止损价位，则以市价单平仓并重置保护状态。

## 风险管理

- **仓位规模**：基础手数按照 `Portfolio.CurrentValue * MaximumRisk / price` 计算；若账户权益不可用，则回退到策略的 `Volume` 属性（默认 1）。
- **连亏减仓**：连续两笔及以上亏损后，手数按 `volume * losses / DecreaseFactor` 减少，完全复刻原 EA 的 `DecreaseFactor` 机制；任意盈利交易会重置亏损计数。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `MaximumRisk` | `0.02` | 每笔交易相对于权益的风险比例。 |
| `DecreaseFactor` | `3` | 连续亏损后的减仓系数，亏损次数除以该值即为减少比例。 |
| `CciPeriod` | `14` | CCI 计算周期。 |
| `CciLevel` | `100` | CCI 超买/超卖阈值。 |
| `CciCurrentBar` | `0` | 当前 CCI 数值的偏移（0 代表最新 K 线）。 |
| `CciPreviousBar` | `1` | 前一 CCI 数值的偏移。 |
| `MaPeriod` | `120` | 趋势过滤 MA 的周期。 |
| `MaMethod` | `Simple` | MA 平滑方式（Simple、Exponential、Smoothed、LinearWeighted）。 |
| `MaCurrentBar` | `0` | MA 的偏移量。 |
| `MaDelta` | `0.001` | 当前与前一 MA 之间的最小斜率差。 |
| `StopLossPips` | `0` | 初始止损距离（单位：点），0 表示禁用。 |
| `TrailingStopPips` | `5` | 基础跟踪止损距离（点）。 |
| `TrailingStepPips` | `5` | 推进跟踪止损所需的最小改善（点）。 |
| `CandleType` | `30m` 时间框 | 策略订阅的主要 K 线类型。 |

## 实现说明

- 内部缓存指标数列，以便按偏移读取历史值，等效于 MQL 中对指标缓冲区的访问方式。
- 点值从 `Security.PriceStep` 推导；若无法获得有效步长，则止损与跟踪功能被视为禁用。
- 代码中的注释全部使用英文，符合仓库要求。
- 按需求暂不提供 Python 版本。
