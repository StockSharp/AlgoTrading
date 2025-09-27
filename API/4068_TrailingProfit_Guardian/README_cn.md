# Trailing Profit 策略

## 概览
Trailing Profit 策略将 MetaTrader 4 专家顾问 `trailing_profit.mq4` 移植到 StockSharp 平台。原始脚本并不会开仓，它只跟踪当前所有订单的浮动收益。当收益超过预设值后启动利润跟踪，如果收益回撤超过给定百分比，就立即平掉全部仓位以锁定收益。StockSharp 版本保持相同的交易思想，同时利用高层 API 完成行情订阅、日志输出和保护模块集成。

## 工作流程
1. 策略通过 `SubscribeTrades()` 订阅逐笔成交，不断获取目标证券的最新成交价。
2. 每收到一笔成交，就按 `Position * (lastPrice - PositionPrice)` 计算浮动盈亏。若当前没有仓位，所有跟踪状态都会被重置。
3. 当浮动收益严格大于 `ActivationProfit` 时，策略开始执行跟踪逻辑。初始的利润底线计算为 `profit - profit * TrailPercent / 100`，表示至少要保留的收益。
4. 每当浮动收益创出新高时，就用同样的公式提升底线，这与 MQL 脚本中更新 `profit_off = profit - profit*(percent/100)` 的行为完全一致。
5. 如果浮动收益跌破当前底线，策略会发出市价单平掉全部仓位。只要剩余持仓量发生变化（例如部分成交），市价单就会再次发送，直到仓位被完全关闭。
6. 当仓位回到零后，跟踪状态（触发标志、底线数值、辅助变量）会被清空，等待下一次手动或其他策略开仓后重新启动跟踪。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `TrailPercent` | `33` | 利润回撤百分比。数值为 `33` 时代表保留 67% 的最大浮动收益。 |
| `ActivationProfit` | `1000` | 启动利润跟踪所需超过的浮动收益。 |

## 实现细节
- `GetWorkingSecurities` 只返回 `(Security, DataType.Ticks)`，让策略在每笔成交上触发，而不依赖于 K 线收盘。
- `ProcessTrade` 直接移植自 `trailing_profit.mq4` 的主循环：负责激活跟踪、抬高底线并触发清仓，同时输出详细日志说明状态变化。
- `ExecuteLiquidation` 记录最近一次平仓指令的方向和数量，避免重复发送完全相同的市价单，并在剩余仓位变化时再次提交。
- `OnPositionChanged` 在仓位归零时重置所有状态，对应于 MQL 中 `close_start` 清空变量的行为。
- 在 `OnStarted` 中调用 `StartProtection()`，可以结合 StockSharp 的保护模块（止损、止盈等）一起使用，尽管核心逻辑已经负责锁定利润。

## 使用方法
将策略附加到需要保护的持仓品种上。该模块不会自行产生开仓信号，只负责管理已有仓位，可与其他入场策略或人工交易配合，自动在达到目标利润后锁定收益。
