# VirtPO TestBed Scalp 策略

本策略将 MetaTrader 4 的 **VirtPOTestBed_ScalpM1** EA 移植到 StockSharp 的高级 API。核心思路保持不变：利用 *虚拟挂单*，在随机指标穿越阈值时“布单”，在价格动量确认后立即转化为市价单执行。所有过滤条件、仓位管理和时间控制均按原始 EA 的逻辑改写。

## 策略逻辑

1. **虚拟挂单阶段**（无持仓时在每根完成的 K 线上运行）：
   * 使用 Level1 最优买卖价，确保点差低于 `SpreadMaxPips`；
   * 最近三根 K 线的平均成交量必须大于 `VolumeLimit`；
   * `VolatilityPeriod` 根 K 线的平均实体长度（点数）大于 `VolatilityLimit`；
   * 布林带宽度（周期 `BollingerPeriod`，宽度 2）必须处于 `BollingerLowerLimit` 与 `BollingerUpperLimit` 之间；
   * 当前时间位于允许的交易窗口（`EntryHour` 起始，持续 `OpenHours` 小时），并且不在禁止的周日 (`Day1`、`Day2`、周五 `FridayEndHour` 之后)；
   * 快速/慢速 SMA 的差值（点数）绝对值大于 `SmaDifferencePips`；
   * 上一根 K 线的实体长度小于 `LastBarLimitPips`。

   当上述过滤全部通过时，检测随机指标交叉：
   * `%K` 向上穿越 `StochasticSetLevel` → 在买价上方 `PoThresholdPips` 点布置虚拟 buy stop；
   * `%K` 向下穿越 `100 - StochasticSetLevel` → 在买价下方同样距离布置虚拟 sell stop。
   虚拟挂单会记录到期时间 (`PoTimeLimitMinutes`) 以及对应的止损 (`StopLossPips`) 与止盈 (`TakeProfitPips`) 距离。

2. **执行阶段** – 若启用 `TickLevel`，策略订阅逐笔成交，一旦最新成交价突破虚拟挂单价格立即触发市价单；若关闭 `TickLevel`，则在每根 K 线收盘时检查。触发后对应虚拟挂单被清除。

3. **风险管理** – 持仓后持续监控：
   * 固定的止损、止盈价格（按点数转换）；
   * 可选的移动止损 `TrailingStopPips`，跟随开仓后的最高/最低价；
   * 持仓时间上限 `CloseTimeMinutes`，到期后根据 `ProfitType` 选择平仓全部/仅盈利/仅亏损仓位。

所有点数都会根据品种的 `PriceStep` 和小数位自动换算（兼容五位报价）。`OrderVolume` 用作每次下单的默认数量，持仓归零时内部状态会自动重置。

## 注意事项

* 必须订阅 Level1 数据，否则无法获得点差和触发价，过滤条件会阻止交易。
* `TickLevel=false` 时执行以 K 线收盘为准，更适合回测；开启后可获得与原 EA 相近的逐笔执行行为。
* 策略只维护单一净头寸，对应 MT4 版本限制同一品种仅持有一笔市价单。

## 参数说明

| 分组 | 参数 | 说明 |
| --- | --- | --- |
| General | Candle Type | 订阅的蜡烛类型（默认 1 分钟）。 |
| Execution | Tick Level | 是否使用逐笔成交触发虚拟挂单。 |
| Execution | PO Threshold (pips) | 虚拟挂单距离买价的点数。 |
| Execution | PO Lifetime (min) | 虚拟挂单的有效时间。 |
| Filters | Max Spread (pips) | 允许的最大点差。 |
| Filters | Volume Limit | 最近三根 K 线的平均成交量阈值。 |
| Filters | Volatility Period | 计算平均实体长度的样本数量。 |
| Filters | Volatility Limit | 平均实体长度的最小值（点）。 |
| Filters | Bollinger Period | 布林带周期。 |
| Filters | Bollinger Lower / Upper | 布林带宽度允许区间（点）。 |
| Filters | Last Bar Limit | 上一根 K 线实体的最大长度。 |
| Trend | Fast/Slow SMA | 趋势过滤所用的快/慢均线周期。 |
| Trend | SMA Difference | SMA 之间的最小差值（点）。 |
| Stochastic | %K / %D / Smooth | 随机指标的周期设置。 |
| Stochastic | Stochastic Set | 激活虚拟挂单的阈值。 |
| Stochastic | Stochastic Go | 执行虚拟挂单的阈值。 |
| Trading | Order Volume | 下单数量。 |
| Risk | Take Profit / Stop Loss / Trailing Stop | 止盈、止损、移动止损的点数。 |
| Schedule | Disable Days, Day1, Day2 | 禁止交易的星期（99 表示不禁用）。 |
| Schedule | Entry Hour / Open Hours | 交易窗口开始时间与持续时长。 |
| Schedule | Friday Cut-off | 周五停止交易的小时。 |
| Risk | Max Lifetime | 持仓时间上限（≥5000 视为关闭）。 |
| Risk | Profit Filter | 时间到期后的平仓规则：0 全部，1 盈利单，2 亏损单。 |

## 与原版差异

* MQL 中的 `CPO` 类被内部状态替代，当价格穿越阈值时直接调用 `BuyMarket` / `SellMarket`。
* 止损/止盈在回测中通过蜡烛高低价验证，实时模式下若有逐笔数据可获得更精细的触发；不支持部分成交与对冲仓位。
* 原 EA 的 `GLots` 动态手数未移植，改为使用固定的 `OrderVolume`。

这些调整确保策略在 StockSharp 的单净持仓模型中仍能重现原有交易思路。
