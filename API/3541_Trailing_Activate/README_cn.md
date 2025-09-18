# Trailing Activate 策略

## 概述
- **来源**：MetaTrader 5 专家顾问“Trailing Activate.mq5”。
- **目标**：在 StockSharp 中复刻原始 EA 的阶梯式移动止损算法，用于管理已经持有的仓位。
- **类型**：辅助型组件。策略不会主动开仓，只负责移动虚拟止损线并在必要时以市价单离场。

## 运行逻辑
1. 策略持续观察当前多/空仓位数量，一旦仓位归零便立即清除内部的跟踪状态。
2. 只有在浮动盈利达到阈值后才会启动移动止损：价格需要至少前进 `TrailingStopPoints + TrailingStepPoints`，同时新的止损价位距离入场价不得低于 `TrailingActivatePoints`。
3. 止损以固定步长上移/下移，只有当改进幅度不小于 `TrailingStepPoints` 时才会接受新的止损价。所有点值都会根据标的物的 `PriceStep` 转换为真实价格间距。
4. 当价格触及跟踪止损时，通过 `BuyMarket`/`SellMarket` 立即平仓，以模拟 MQL 中的 `PositionModify` 行为。由于 StockSharp 无法直接修改经纪商端止损，因此采用市价离场的方式实现相同的结果。
5. 支持两种更新模式：
   - **EveryTick**：基于 Level1 最优买卖价的每笔更新，对应 EA 中的 `bar_0` 模式，可在每次报价变动时移动止损。
   - **NewBar**：仅在所选时间框架的蜡烛收盘时重新计算，对应 `bar_1`，适合需要按周期批量移动的场景。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `TrailingMode` | 选择按 tick 还是按收盘蜡烛更新止损。 | `NewBar` |
| `CandleType` | 当选择 `NewBar` 时用于订阅蜡烛的时间框架。 | 15 分钟蜡烛 |
| `TrailingActivatePoints` | 启动跟踪止损所需的盈利点数。 | `70` |
| `TrailingStopPoints` | 价格与止损之间保持的点数距离。 | `250` |
| `TrailingStepPoints` | 每次移动止损所需的额外盈利点数。 | `50` |

所有点数参数都会乘以 `Security.PriceStep` 转换为绝对价格差。如果交易品种未提供价格步长或数值为 0，将退化为 1，以保持与原 EA 相同的容错处理。

## 实现细节
- 策略同时订阅蜡烛和 Level1 数据，但只有与当前 `TrailingMode` 对应的那条数据流会驱动逻辑，从而在使用 StockSharp 高层 API 的同时保持与原版一致的行为。
- MQL 中依赖的 `SYMBOL_TRADE_STOPS_LEVEL`、`SYMBOL_TRADE_FREEZE_LEVEL` 在 StockSharp 中无法直接查询，因此转换版本只按照用户设定的距离进行检查，并在文档中注明该差异。
- 以 `Position.AveragePrice` 作为入场参考价。如果策略附加到已有但缺少平均价的仓位，将临时使用最新价格，直至新的成交信息提供准确数值。
- 跟踪止损为纯虚拟实现，触发后通过市价单平仓。如需券商端的真实止损，可在策略之外单独挂单。

## 使用步骤
1. 在需要管理的标的上启动策略。
2. 确保已有仓位（手动或其他策略开仓）。当浮盈满足设定阈值后，移动止损会自动生效。
3. 根据品种最小变动价位调整三个点数型参数，以获得期望的跟踪行为。
4. 高频行情选择 `EveryTick`，若希望按照周期收盘移动则选择 `NewBar`。
