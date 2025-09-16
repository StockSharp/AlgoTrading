# Trade Panel 策略

## 概述
该策略使用 StockSharp 的高级 API 复刻了 MetaTrader 上的 "TradePanel" 专家顾问。原始脚本提供了一套图形化面板，用于手动提交市价单、按不同模式平仓并维护基础的止损/止盈。转换后的版本用策略参数替代了界面，但在执行流程与风险控制上保持与 MQL 脚本一致的意图。

## 运行逻辑
1. **行情订阅**：策略订阅目标标的的 Level1 数据，并缓存最新成交价、买一价和卖一价，以便评估止损、止盈以及浮动盈亏。
2. **手动指令**：`BuyRequest`、`SellRequest`、`CloseRequest` 三个布尔参数模拟面板按钮。当参数被设为 `true` 时发送对应的市价单，并立即将参数重置为 `false`。只有在连接在线且 `Security`、`Portfolio` 均已设置时才会执行指令。
3. **平仓模式**：`CloseMode` 枚举完全对应面板逻辑：
   - `CloseAll`：一次性平掉全部净头寸。
   - `CloseLast`：按 `OrderVolume` 的数量平仓，模拟关闭最近一笔交易的规模。
   - `CloseProfit`：仅在当前价格相对平均持仓价 (`PositionPrice`) 有盈利时平掉全部头寸。
   - `CloseLoss`：仅在当前价格导致亏损时平掉全部头寸。
   - `ClosePartial`：平掉 `PartialCloseVolume` 规定的数量，以便手动减仓。
4. **保护逻辑**：止损与止盈以“点”为单位配置，通过 `Security.PriceStep` 转换为价格距离。当市场价格突破阈值时，策略会发送市价单直接平仓。内部标志位防止在头寸尚未变化前重复触发保护指令。

## 参数列表
| 参数 | 说明 |
|------|------|
| `OrderVolume` | 新开仓的默认数量，同时用于 `CloseLast` 模式。 |
| `StopLossPoints` | 止损距离（点），0 表示关闭止损。 |
| `TakeProfitPoints` | 止盈距离（点），0 表示关闭止盈。 |
| `PartialCloseVolume` | 在 `ClosePartial` 模式下需要平掉的数量。 |
| `CloseMode` | 平仓行为枚举（`CloseAll`、`CloseLast`、`CloseProfit`、`CloseLoss`、`ClosePartial`）。 |
| `BuyRequest` | 设为 `true` 时发送买入市价单（自动复位）。 |
| `SellRequest` | 设为 `true` 时发送卖出市价单（自动复位）。 |
| `CloseRequest` | 设为 `true` 时按照 `CloseMode` 执行平仓（自动复位）。 |

## 与 MQL 版本的差异
- 不再包含图形界面、绘图、声音或速度条，所有交互都通过参数完成。
- StockSharp 基于净头寸管理，因此多个 MetaTrader 订单会在此被汇总为单一头寸。
- 止损和止盈触发时直接以市价单平仓，而不是修改单个订单。
- 面板专用的定时器和鼠标事件被移除，因为在自动化或回测场景中并不需要。

## 使用说明
- 请确保 Level1 行情可用，否则无法计算保护条件。
- 手动指令在收到新的 Level1 更新时进行处理，在市场无波动时可能需要等待下一个报价。
- `StopLossPoints` 与 `TakeProfitPoints` 依赖 `Security.PriceStep`，应按照原始交易品种正确设置价格步长。
- 策略在发送交易之前会检查连接状态以及必要属性，延续了原面板的安全校验逻辑。
