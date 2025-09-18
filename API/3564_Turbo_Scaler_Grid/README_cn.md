# Turbo Scaler 网格策略

## 概述
Turbo Scaler 网格策略是对 MQL5 “Turbo Scaler Grid Pending” EA 的 StockSharp 高级接口实现。策略围绕预设价格水平构建买入/卖出止损网格，并结合多时间框架过滤条件，同时通过移动止损和浮动权益管理来保护已有头寸。

- 触发时间框参数决定何时根据价格靠近锚点价位而激活挂单。
- 30 分钟、2 小时以及日线蜡烛用于提供额外的趋势与区间确认条件。
- Level1 行情提供最新买卖价，用于计算挂单价格以及移动止损。

## 交易规则
1. **挂单网格**
   - 买入/卖出止损挂单以 `BuyStopEntry`、`SellStopEntry` 为起点，并按照 `PendingStepPoints` 的间距扩展。
   - 最大挂单数量由 `PendingQuantity` 控制。
   - 价格触发器会检查触发时间框内最近的蜡烛，确保价格在锚点附近出现动量突破。
   - 条件触发器启用时，会进一步验证日线阻力/支撑区间、H2 与 M30 蜡烛方向以及中位价位置。
2. **仓位保护**
   - 初始止损基于 `StopLossPoints` 计算（或使用 `BuyStopLossPrice`、`SellStopLossPrice` 固定价格）。
   - 当价格向有利方向移动 `BreakevenTriggerPoints` 时，止损被移动到入场价并加上 `BreakevenOffsetPoints` 的缓冲。
   - `TrailPoints` 与 `TrailMultiplier` 控制的移动止损仅在达到保本后才会生效。
3. **权益管理**
   - 如果浮动亏损超过 `MaxFloatLoss`（根据下单手数等比例放大），策略会立即平仓。
   - 当浮盈达到 `EquityTrigger`（同样按手数缩放）时，会生成内部权益锁定线，并以 `EquityTrail` 的距离对其进行追踪；锁定线的起点为 `EquityBreakeven`。

## 参数
| 参数 | 说明 |
| --- | --- |
| `StopLossPoints` | 初始止损点数。 |
| `BreakevenTriggerPoints` | 触发移动止损到保本所需的点数。 |
| `BreakevenOffsetPoints` | 移动到保本位置时附加的点数缓冲。 |
| `TrailPoints` | 达到保本后使用的移动止损距离。 |
| `TrailMultiplier` | 触发下一次移动止损前的放大系数。 |
| `BuyStopLossPrice` / `SellStopLossPrice` | 多/空仓位的固定止损价格。 |
| `BuyStopEntry` / `SellStopEntry` | 买入/卖出止损网格的基准价格。 |
| `OrderVolume` | 每张挂单的下单量。 |
| `PendingQuantity` | 允许同时存在的挂单数量。 |
| `PendingStepPoints` | 相邻挂单之间的距离。 |
| `TriggerCandleType` | 用于价格触发逻辑的蜡烛时间框。 |
| `PendingPriceTrigger` | 是否启用价格靠近触发器。 |
| `PendingConditionTrigger` | 是否启用多时间框确认触发器。 |
| `OrderBuyBlockStart` / `OrderBuyBlockEnd` | 用于验证多头的日线区间上/下界。 |
| `OrderSellBlockStart` / `OrderSellBlockEnd` | 用于验证空头的日线区间上/下界。 |
| `MaxFloatLoss` | 最大允许浮亏（会按下单量缩放）。 |
| `EquityBreakeven` | 浮盈触发后锁定的权益水平。 |
| `EquityTrigger` | 创建权益锁定线所需的浮盈。 |
| `EquityTrail` | 对权益锁定线应用的追踪距离。 |

## 说明
- 策略默认将 0.01 手视为基准手数，并根据 `OrderVolume` 自动缩放权益相关参数。
- 代码中的所有注释均为英文，以便与仓库其他策略保持一致。
- 实现仅使用 StockSharp 的高级 API（`SubscribeCandles`、`Bind`、`BuyStop`、`SellStop` 等），满足项目指导要求。
