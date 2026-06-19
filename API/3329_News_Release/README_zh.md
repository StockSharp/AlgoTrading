# 新闻发布策略

该策略复刻了 **NewsReleaseEA** 智能交易系统的核心思想：在预定的新闻发布时间前后布设对称的挂单，并对触发后的头寸进行主动管理。

## 核心思路

- 五个主要输入（新闻时间、提前/滞后窗口、挂单距离以及层间间距）决定了挂单的时间与位置。
- 在设定的新闻时间之前，策略会提交一组买入止损单与卖出止损单。第一组挂单距离当前买卖价 `DistancePips` 个点，其余组之间通过 `StepPips` 调整间隔。
- 所有挂单会保留至新闻发布后 `PostNewsMinutes` 分钟，窗口结束时会自动撤销全部挂单，并在需要时平掉现有头寸。
- 当任意方向的挂单被触发时，对侧挂单立即撤销，剩余仓位由止损、止盈、保本和追踪四套规则（以点数表示）进行管理。
- 保本逻辑会在价格朝有利方向运行 `BreakEvenTriggerPips` 点后启动，并在价格回落至入场价加减 `BreakEvenOffsetPips` 点时平仓，确保盈利不被完全回吐。
- 追踪管理会记录入场后的最佳价位，一旦当前价格与极值之间的距离超过 `TrailingPips` 点，策略立即平仓以锁定利润。
- `TradeOnce` 参数对应 MQL 程序中的“一次事件仅交易一次”选项，可防止在首次交易结束后再次激活。

## 参数说明

- `NewsTime` – 新闻发布时间。
- `PreNewsMinutes` – 提前多少分钟开始挂单。
- `PostNewsMinutes` – 新闻发布后挂单保留的分钟数。
- `OrderPairs` – 同时提交的买入止损/卖出止损对数。
- `DistancePips` – 第一组挂单距离当前买卖价的点数。
- `StepPips` – 相邻挂单组之间额外的点数间距。
- `OrderVolume` – 每个挂单的下单量。
- `TradeOnce` – 开启后一个事件窗口内仅允许交易一次。
- `UseStopLoss` / `StopLossPips` – 启用并设置止损点数。
- `UseTakeProfit` / `TakeProfitPips` – 启用并设置止盈点数。
- `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` – 配置保本策略。
- `UseTrailing` / `TrailingPips` – 启用追踪平仓逻辑及其点数距离。
- `CloseAfterEvent` – 事件窗口结束后是否强制平仓。

## 备注

- 策略仅使用 Level1 行情数据（`SubscribeLevel1`），无需等待蜡烛收盘即可根据最新买卖价做出反应。
- 点数会根据合约的 `PriceStep` 转换为实际价格距离；若 `PriceStep` 不可用，则退化为 1 作为安全回退值。
- 止损、止盈、保本与追踪规则通过调用 `ClosePosition()` 市价平仓，保持与原始 EA 类似的响应式管理方式。
- 按需求未提供 Python 版本。
