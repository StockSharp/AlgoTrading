# Trailing Star 跟踪止损策略合集

## 来源

原始的 MetaTrader 5 “Trailing Star” 套件由 Pham Ngoc Vinh 编写，包含两个专家顾问：

* **Trailing Star on Point**：当仓位盈利达到设定的点数（points）后启动跟踪止损。
* **Trailing Star on Price**：当市场价格突破用户指定的价格时开始跟踪。

两个顾问都会遍历所有持仓，并调用 `PositionModify`，根据当前买价或卖价将止损移动到固定距离。

## StockSharp 适配

本文件夹提供两个使用高层 API 的 StockSharp 策略，以复刻原有逻辑：

* `TrailingStarPointStrategy`：以 MetaTrader 点数为单位，判断盈利是否达到阈值，并按点差更新止损单。
* `TrailingStarPriceStrategy`：在市场触及固定价格后激活，然后同样以点数保持止损距离。

两种实现都基于 **Level1** 数据，不依赖自定义缓存或指标缓冲区。所有价格、数量都通过策略基类的标准化函数处理，只在新的止损价优于当前止损时才会重新登记订单，与 MQL 版本的条件完全一致。

### 建仓价格追踪

`TrailingStarPointStrategy` 通过 `OnNewMyTrade` 回调中的成交信息重建仓位的平均开仓价：

* 买入成交会优先冲销现有空头持仓，再更新多头平均成本。
* 卖出成交会优先冲销现有多头持仓，再更新空头平均成本。
* 当净仓位归零时，所有跟踪数据与止损订单都会被重置。

该流程与 MetaTrader 版本遍历 `PositionsTotal()` 并读取 `POSITION_PRICE_OPEN` 的效果相同。

`TrailingStarPriceStrategy` 无需记录成交价格，因为触发价由用户直接给出。

### Level1 数据的使用

策略通过 `SubscribeLevel1().Bind(...)` 订阅最优买卖价，等同于 MQL 中的 `latest_price.bid/ask`。只有在收到新的 Level1 数据时才会计算跟踪逻辑，如果行情缺失，策略会跳过当前更新，避免在流动性不足的品种上出现错误操作。

### 止损订单管理

在 MetaTrader 中，`PositionModify` 会修改现有止损。StockSharp 实现改为维护单一保护性订单：

1. 当不存在保护性订单时，按照仓位方向发送 `SellStop`（多头）或 `BuyStop`（空头）。
2. 如果订单仍在活动状态，只会在新价格更有利（多头止损更高、空头止损更低）时重新登记。
3. 若订单完成、失败或被取消，会重新创建新的保护性订单。

当仓位关闭时，策略会取消所有保护性订单并清空内部状态，与 MQL 版本中跳过已平仓持仓的行为一致。

## 参数

| 策略 | 参数 | 说明 |
| --- | --- | --- |
| `TrailingStarPointStrategy` | `EntryPointPips` | 激活跟踪止损前所需的最低盈利点数。 |
| | `TrailingPointPips` | 跟踪止损与当前价格之间保持的点数距离。 |
| `TrailingStarPriceStrategy` | `EntryPrice` | 市场价格突破该值后开始启动跟踪止损。 |
| | `TrailingPointPips` | 跟踪止损与当前价格之间保持的点数距离。 |

所有参数均通过 `StrategyParam<T>` 暴露，并附带 `SetDisplay` 元数据，方便在 StockSharp UI 与优化器中配置。

## 使用建议

1. 在启动策略前，设置好目标证券与投资组合。
2. 确保行情源能够提供最优买卖价的 Level1 数据。
3. 根据交易品种的点值配置参数。策略会利用证券的 `PriceStep` 与小数位信息把 MetaTrader 点数转换为实际价格增量。
4. 启动策略后，它只会管理现有仓位的止损，不会主动开仓。

> **重要提示：** 策略仅负责保护现有仓位，不包含任何入场逻辑。如需自动交易，可与其它策略或人工下单配合使用。

## 与原始 MQL 版本的差异

* 使用 StockSharp 的保护性订单（`SellStop`/`BuyStop`）替代 `PositionModify`。
* 借助成交回报跟踪仓位成本，而不是遍历 MetaTrader 的全局持仓列表。
* 策略只管理单一证券，符合 StockSharp 架构，简化风险控制；MQL 版本会遍历所有品种的持仓。

这些调整保留了核心跟踪逻辑，同时符合 StockSharp 的最佳实践。
