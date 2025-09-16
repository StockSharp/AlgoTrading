# 日线 STP 入场框架策略（StockSharp 版本）

## 策略简介
该策略基于 MetaTrader EA “Daily STP Entry Frame”，使用 StockSharp 高级 API 重新实现。系统在每天开盘后，根据上一交易日的高点和低点预先放置突破方向的止损挂单，并通过多重过滤条件确保当前价格已经接近关键区间。策略特别适用于外汇品种，默认把“基点”视为五位报价下的 0.0001。

## 工作流程
1. **记录昨日区间**：订阅日线级别的 K 线，持续保存上一根完成蜡烛的最高价和最低价。
2. **实时监控**：订阅 Level1 行情，获取即时的买价、卖价与最新成交价，用于下单与仓位管理。
3. **挂单条件**：当新交易日开始且满足以下约束时提交挂单：
   - 最新价与昨日高/低的距离不少于 `ThresholdPoints`；
   - 当天开盘价位于突破方向所需的一侧；
   - 买入止损价 = 昨日高点 + `SpreadPoints / 2`；卖出止损价 = 昨日低点 − `SpreadPoints / 2`。
4. **风险过滤**：若账户回撤超过 `MaximumDrawdownPercent`，或时间过滤（周末、时段、指定日）不满足，则不会创建新挂单。
5. **仓位管理**：成交后执行以下保护措施：
   - 固定止损与止盈（单位为基点，自动转换为价格距离）；
   - 按 `CloseAfterSeconds` 设置的秒数强制平仓；
   - 当 `TrailingSlope < 1` 时启用拖尾止损，按原 EA 的斜率公式动态推进保护价位。
6. **日终处理**：到达 `NoNewOrdersHour`（周五使用专门的 `NoNewOrdersHourFriday`）或跨过自然日时，立即撤销尚未成交的挂单。

## 入场与风险规则
- **做多条件**
  - `SideFilter` 为 0（双向）或 1（仅多）。
  - 昨日高点 − 当前价格 ≥ `ThresholdPoints`。
  - 今日开盘价低于昨日高点。
  - 目标买入价需与当前卖价保持最小距离。
- **做空条件**
  - `SideFilter` 为 0（双向）或 -1（仅空）。
  - 当前价格 − 昨日低点 ≥ `ThresholdPoints`。
  - 今日开盘价高于昨日低点。
  - 目标卖出价需与当前买价保持最小距离。
- **资金管理**
  - 依据账户累计盈利的 `PercentOfProfit` 百分比计算仓位。
  - 挂单手数始终限制在 `MinVolume` 与 `MaxVolume` 之间，并根据 `VolumeStep` 进行对齐。
  - 当回撤超过 `MaximumDrawdownPercent` 时停止生成新挂单。
- **保护机制**
  - 止损、止盈均以基点定义，并按照品种的点值转换为实际价格。
  - 拖尾止损遵循原 EA 的公式：多头 `Stop = Bid - StopLoss - Slope * (Bid - Entry)`，空头逻辑对称。
  - `CloseAfterSeconds` 可实现到时强制平仓。

## 主要参数
| 参数 | 说明 |
| --- | --- |
| `CandleType` | 计算昨日区间的时间框架（默认日线）。 |
| `StopLossPoints` / `TakeProfitPoints` | 止损、止盈距离（基点）。 |
| `TrailingSlope` | 拖尾比例，≥1 表示禁用。 |
| `SideFilter` | -1 仅空、0 双向、1 仅多。 |
| `ThresholdPoints` | 距离门槛，决定是否挂单。 |
| `SpreadPoints` | 用于补偿点差的附加位移。 |
| `SlippagePoints` | 验证最小距离时的额外缓冲。 |
| `NoNewOrdersHour` / `NoNewOrdersHourFriday` | 正常日与周五撤单时间。 |
| `EarliestOrderHour` | 允许开始挂单的最早小时。 |
| `DayFilter` | 6 表示全周，其余 0–5 对应周日到周五。 |
| `CloseAfterSeconds` | 仓位存续秒数，0 表示关闭功能。 |
| `PercentOfProfit` | 按盈利比例调整仓位。 |
| `MinVolume` / `MaxVolume` | 挂单数量上下限。 |
| `MaximumDrawdownPercent` | 最大允许回撤百分比。 |

## 实现细节
- 若合约的 `Decimals` 为 3 或 5，基点按 `PriceStep * 10` 计算，与原始 EA 一致。
- 每个交易日更替时自动撤销旧挂单，避免重复下单。
- 日终撤单逻辑保留了周五单独设置的截止时间。
- Equity 邮件提醒被日志输出取代，便于在 StockSharp Designer/Runner 中查看。
- 即使已有持仓仍允许新的挂单保持激活，忠实重现原策略风格。

## 使用建议
- 在真实交易前，通过 StockSharp Designer 进行参数回测与优化。
- 确保所选品种已正确设置 `PriceStep`、`StepPrice` 与 `VolumeStep`，否则点值换算会出现误差。
- 可结合平台的组合风控、滑点模拟等功能强化风险控制。

