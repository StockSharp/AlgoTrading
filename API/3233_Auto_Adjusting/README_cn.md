# AutoAdjustingStrategy

AutoAdjustingStrategy 将 MetaTrader 专家顾问 *Aouto Adjusting1* 迁移到 StockSharp 的高级 API。策略保留了原始的多周期动量过滤、月线 MACD 趋势确认以及三重 EMA 结构，用于识别趋势内回调。止损与止盈基于最近摆动高低点计算，并在每根完成的 K 线后自动更新。

## 策略逻辑

1. **趋势结构**：交易周期上的三条 EMA（6、14、26）必须同向排列（做多要求 `EMA6 < EMA14 < EMA26`，做空相反）。最近一根已完成 K 线需要触碰中间 EMA，再上一根 K 线形成更高的低点 / 更低的高点以确认回调。
2. **动量确认**：在更高周期（依据交易周期映射，例如 H1→D1）上计算的 Momentum 指标需在最近三根完成 K 线中任意一次相对 100 偏离至少 `MomentumBuyThreshold` / `MomentumSellThreshold`。
3. **宏观过滤**：月线 MACD(12, 26, 9) 用于确认大趋势方向（`MACD > Signal` 允许做多，`<` 允许做空）。
4. **执行**：当所有过滤条件满足且不存在反向持仓时，以市价开仓；若存在反向仓位，先平仓再开新单。
5. **风险防护**：止损设置在最近 `CandlesBack` 根 K 线最低点/最高点之外 `PadAmount` 个点，止盈距离为止损距离乘以 `RewardRatio`。持仓期间，每根 K 线收盘都会重新布置保护单。

## 风险管理与仓位

- `RiskPercent` 会在获取到组合权益和价格步长数据时，按风险金额 / 单位亏损的方式计算自适应手数。
- 若无法完成风险计算（例如缺少权益信息），则使用固定的 `TradeVolume` 作为下单量。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `TimeFrame(H1)` | 交易周期，三条 EMA 在此周期上计算。 |
| `MomentumCandleType` | `DataType` | 基于 `CandleType` 映射 | 动量指标所用的更高周期（H1→D1、H4→W1 等）。 |
| `MacroMacdCandleType` | `DataType` | `TimeFrame(30 days)` | 宏观 MACD 所用的时间框架，默认约等于月线。 |
| `PadAmount` | `decimal` | `3` | 在摆动低/高点之外追加的点数，用于设置止损。 |
| `RiskPercent` | `decimal` | `0.1` | 单笔交易允许的权益风险百分比。 |
| `RewardRatio` | `decimal` | `2` | 止盈距离 = 止损距离 × 此系数。 |
| `CandlesBack` | `int` | `3` | 回溯多少根 K 线来寻找最新的高低点。 |
| `MomentumBuyThreshold` | `decimal` | `0.3` | 触发多头信号所需的最小动量偏离值。 |
| `MomentumSellThreshold` | `decimal` | `0.3` | 触发空头信号所需的最小动量偏离值。 |
| `TradeVolume` | `decimal` | `1` | 当无法计算风险仓位时的备用下单手数。 |

## 可视化建议

- 在主图上绘制交易周期的 K 线与三条 EMA，观察价格回调情况。
- 在副图展示更高周期的 Momentum 指标，检查是否达到阈值。
- 关注宏观周期的 MACD 数值，确认趋势过滤方向。

## 备注

- 时间框架映射遵循原始 EA：M1→M15、M5→M30、M15→H1、M30→H4、H1→D1、H4→W1、D1→MN1，其余周期保持不变。
- 策略不调用指标的 `GetValue`，而是通过 `Bind` 回调保存最新值，符合项目要求。
- 按原策略思路，每根 K 线收盘后都会重新计算并布置止损与止盈，实现“自动调节”的行为。
