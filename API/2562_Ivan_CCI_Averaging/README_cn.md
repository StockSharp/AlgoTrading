# Ivan CCI Averaging 策略
[English](README.md) | [Русский](README_ru.md)

将 MetaTrader “Ivan” 智能交易系统移植到 StockSharp 的版本，使用 CCI 极值信号结合加仓与平滑移动平均止损。策略监控长期 CCI(100) 以确认全局多空方向，可在 CCI(13) 回调时按需加仓，并通过平滑均线的止损、保本和跟踪逻辑管理风险。仓位规模沿用原策略的账户风险百分比模型，利润保护系数在权益倍增时强制平仓。

## 细节

- **入场条件**：
  - **多头全局信号**：当 CCI(100) 上穿 `GlobalSignalLevel` 且没有活跃的买入状态时，按市价做多，并把初始止损设在平滑均线上，前提是止损至少低于价格 `MinStopDistance`。
  - **多头加仓**：在启用 `UseAveraging` 且全局多头标志为真时，只要 CCI(13) 跌破 `-GlobalSignalLevel` 就按同样模板再加一笔多头。
  - **空头全局信号**：当 CCI(100) 下穿 `-GlobalSignalLevel` 且没有活跃的卖出状态时，按市价做空，只要均线止损至少高出价格 `MinStopDistance`。
  - **空头加仓**：启用 `UseAveraging` 时，全局空头状态下只要 CCI(13) 上穿 `GlobalSignalLevel` 就增加空单。
- **多空方向**：双向交易，并可在当前偏向内金字塔加仓。
- **出场条件**：
  - CCI(100) 回到 `±ReverseLevel` 区间内会清除多空标志并强制平仓。
  - 投资组合权益超过初始权益的 `ProfitProtectionFactor` 倍时立即平仓锁定收益。
  - 达到跟踪止损（保本价或更新后的均线止损）时平掉相应方向。
- **止损**：
  - 初始止损来自周期为 `StopLossMaPeriod` 的平滑移动平均（SMMA）。
  - 当价格运行 `BreakEvenDistance` 后，止损移动到入场价（设为 0 可关闭保本功能）。
  - 只有当均线上移/下移超过 `TrailingStep` 时才会推进跟踪止损。
- **过滤条件**：
  - `UseZeroBar` 复刻 MT5 选项，可选择使用刚开启的当前 K 线或上一根收盘 K 线的数值。
  - `MinStopDistance` 防止均线止损过于靠近入场价。
- **仓位规模**：
  - 每次下单都会以 `RiskPercent` 的账户权益除以入场价与止损价的差值来计算手数，`MinimumVolume` 作为下限。

## 参数

- **Use Averaging** *(bool，默认: true)* — 是否允许在全局信号下进行加仓。
- **Stop MA Period** *(int，默认: 36)* — 平滑均线的周期，用于生成止损。
- **Risk %** *(decimal，默认: 10)* — 每次交易愿意承担的账户权益百分比。
- **Use Zero Bar** *(bool，默认: true)* — 是否使用当前形成的 K 线数据；为 false 时使用上一根收盘 K 线。
- **Reverse Level** *(decimal，默认: 100)* — CCI 回撤到该绝对值以内时清除所有仓位。
- **Global Level** *(decimal，默认: 100)* — 触发全局买入或卖出信号的 CCI 绝对阈值。
- **Min Stop Distance** *(decimal，默认: 0.005)* — 入场价与均线止损之间的最小价差（0.005 ≈ 外汇五位报价的 50 点）。
- **Trailing Step** *(decimal，默认: 0.001)* — 推进跟踪止损所需的最小均线进展。
- **BreakEven Distance** *(decimal，默认: 0.0005)* — 把止损移到入场价所需的利润幅度；0 表示禁用。
- **Profit Protection** *(decimal，默认: 1.5)* — 当权益达到该倍数时强制平仓以保护利润。
- **Minimum Volume** *(decimal，默认: 1)* — 风险模型计算出过小手数时使用的最小交易量。
- **Candle Type** *(DataType)* — 指标使用的 K 线类型（默认 15 分钟）。

## 备注

- `MinStopDistance`、`TrailingStep` 和 `BreakEvenDistance` 以价格单位表示，应根据标的的最小变动价位调整。
- 策略假设 `BuyMarket`/`SellMarket` 指令即时成交；若预期滑点或部分成交，请调整执行设置。
- 需要可用的投资组合适配器才能进行基于权益的仓位计算，否则始终使用 `MinimumVolume`。
