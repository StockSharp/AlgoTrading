# OverHedge V2 网格策略
[English](README.md) | [Русский](README_ru.md)

该策略在 StockSharp 高级 API 上复刻了 MetaTrader 的 OverHedge V2 智能交易系统。策略先使用快、慢 EMA 判断方向，然后在动态隧道内交替开仓，构建对冲网格。每次加仓按几何级数放大手数，当整篮头寸的浮动利润达到设定目标后统一平仓。

## 交易逻辑

- **趋势过滤器：** 只有当 8 周期 EMA 与 21 周期 EMA 的差值超过 `MinDistancePips` 时才允许开仓，并确定新循环的首笔方向。
- **价格隧道：** 隧道宽度等于当前点差的两倍加上 `TunnelWidthPips`（按最小价位换算），用于触发对冲方向的挂点。
- **方向交替：** 前三笔沿趋势方向加仓，之后开始交替买卖，使持仓在隧道两端形成对冲结构。
- **手数放大：** 从 `StartVolume` 起，每笔订单都会乘以 `BaseMultiplier`，并根据交易品种的最小手数约束进行调整。
- **循环退出：** 当每个方向的浮动盈利至少达到 `MinProfitTargetPips` 且篮子总盈利超过 `ProfitTargetPips` 时，全部头寸市价平仓并重置状态。
- **手动关闭：** 将 `ShutdownGrid` 设为 `true` 会立即平掉当前持仓并阻止新订单，直到重新关闭该开关。

## 入场条件

### 做多
- 趋势过滤器指示多头 (`EMA_short - EMA_long > MinDistancePips`)。
- 最新卖价（Ask）不低于当前买入锚点。
- 未触发停用开关且盈利目标尚未达到。

### 做空
- 趋势过滤器指示空头 (`EMA_long - EMA_short > MinDistancePips`)。
- Ask 不高于当前卖出锚点。
- `ShutdownGrid` 为假且篮子尚未达到目标利润。

## 平仓管理

- **盈利退出：** 当篮子浮盈满足 `ProfitTargetPips` 且两侧至少获得 `MinProfitTargetPips` 的单边收益时，全部仓位以市价平仓。
- **紧急退出：** 手动把 `ShutdownGrid` 设为 `true` 会立即关闭所有头寸。

## 指标与数据

- 使用 8 周期 EMA 和 21 周期 EMA 计算趋势，基于所选 K 线类型。
- 订阅 Level 1 行情以获取最佳买卖价，用于计算隧道宽度并判断触发条件。

## 参数说明

| 参数 | 含义 |
|------|------|
| `StartVolume` | 每个循环首单的交易量。 |
| `BaseMultiplier` | 每次加仓时应用的几何放大系数。 |
| `TunnelWidthPips` | 相对于双倍点差额外增加的隧道宽度（点）。 |
| `ProfitTargetPips` | 篮子总盈利目标（以点换算）。 |
| `MinProfitTargetPips` | 在允许平仓前，每个方向至少需要的盈利幅度。 |
| `ShortEmaPeriod` | 快速 EMA 的周期。 |
| `LongEmaPeriod` | 慢速 EMA 的周期。 |
| `MinDistancePips` | 判定趋势所需的 EMA 最小差值。 |
| `CandleType` | 供指标和交易循环使用的 K 线类型。 |
| `ShutdownGrid` | 强制关闭开关，启用后清仓并阻止新交易。 |

## 实践提示

- 默认 K 线为 1 小时，可根据原策略的时间框架调整。
- 需要提供 Level 1 行情，否则无法正确估算点差和入场条件。
- StockSharp 采用净持仓模式，交替买卖会减少或翻转净头寸，而不是持有多个独立的对冲单，但整体盈亏逻辑仍保持一致。
- 在实盘或回测前请确认交易品种的最小变动价位与手数，以便隧道和手数扩张设置符合市场规则。
