# ADX System DI 交叉策略

## 概述
ADX System 策略是 MetaTrader 4 专家顾问 `ADX_System.mq4` 的 StockSharp 版本。原始 EA 会比较最近两根收盘蜡烛的
平均方向性指数（Average Directional Index，简称 ADX）以及它的 +DI 和 -DI 分量。当 +DI 上穿 ADX 时，系统希望
做多；当 -DI 上穿 ADX 时，系统希望做空。移植版本通过缓存最近两根闭合蜡烛的指标值，完全复刻了
MetaTrader 中调用 `iADX(..., shift=1/2)` 的行为。

策略始终只保持一张净头寸。进场与出场都使用市价单，这与 MetaTrader 在净头寸模式下的“单票”逻辑一致。
风险控制与原始专家顾问保持同步：止盈与止损以点数形式相对于平均建仓价设置，另外还提供可选的
移动止损，在价格向有利方向运行后锁定利润。

## 交易逻辑
1. 订阅指定的时间框架（`CandleType`），只处理已经收盘的蜡烛，避免在蜡烛尚未完成时做出决策。
2. 将蜡烛数据传入 `AverageDirectionalIndex` 指标，并等待其输出 ADX、+DI 和 -DI 数值。
3. 缓存最近两根收盘蜡烛的指标值，使策略能够像原始 EA 一样读取“当前”和“前一根”数据对。
4. **做多条件**：如果较旧的 ADX（`shift = 2`）低于较新的 ADX（`shift = 1`），较旧的 +DI 低于该较旧的 ADX，
   而较新的 +DI 高于较新的 ADX，则发送买入市价单。
5. **做空条件**：当上述条件作用于 -DI 时（旧 -DI 低于旧 ADX，新 -DI 高于新 ADX），发送卖出市价单。
6. **多头退出**：当 ADX 开始下降且 +DI 再次跌破 ADX，或触发止盈、止损、移动止损时，立即平仓。
7. **空头退出**：完全镜像多头退出逻辑，只是使用 -DI 分量与风险参数。
8. 每根蜡烛结束后更新指标历史，确保下一次判断使用最新的 `shift = 1/2` 数值对。

## 风险管理
- `TakeProfitPoints` 与 `StopLossPoints` 以 MetaTrader 风格的点数表示距离。如果 `Security.PriceStep` 可用，则
  会转换为真实价格差；否则直接将其视作绝对价差。
- 移动止损（`TrailingStopPoints`）只有在浮盈超过设定距离后才会激活。激活后，止损价只会朝盈利方向移动，
  一旦价格突破该水平即平仓。
- 所有离场（指标反转、止盈、止损、移动止损）都通过市价单执行，从而立即平掉头寸，等效于原始 EA 的
  `OrderClose` 调用。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 1 分钟 | 策略处理的主时间框架。 |
| `AdxPeriod` | `int` | `14` | 计算 ADX 及其 DI 分量所需的蜡烛数量。 |
| `TradeVolume` | `decimal` | `1` | 每次市价下单的手数。 |
| `TakeProfitPoints` | `decimal` | `100` | 相对入场价的止盈距离（点）。 |
| `StopLossPoints` | `decimal` | `30` | 相对入场价的止损距离（点）。 |
| `TrailingStopPoints` | `decimal` | `0` | 可选的移动止损距离（点），设为 0 表示关闭移动止损。 |

## 与原始 MetaTrader 专家顾问的差异
- MetaTrader 管理单独的订单票据，而 StockSharp 使用净头寸模式。因此，当信号翻转时，移植版本会先平掉
  当前持仓再发送新的进场指令。
- 原始 EA 依赖 `Point` 将点数转换为价格。StockSharp 版本在可用时使用 `Security.PriceStep`；若不可用，则把
  参数视作绝对价格差，因此在价格步长非常规的品种上可能需要调整默认值。
- MetaTrader 通过修改已有订单来实现移动止损。StockSharp 版本在价格突破移动止损价位时直接以市价平仓，
  在净头寸模型下这种方式更加简单但效果等同。

## 使用建议
- 请确认 `TradeVolume` 与交易标的的最小交易量步长匹配。构造函数同时把该值赋给 `Strategy.Volume`，因此
  内置下单助手会使用正确的手数。
- 如果标的波动较大或报价步长较小，可以相应提高 `TakeProfitPoints` 和 `StopLossPoints`。
- 可将策略添加到图表中，配合 ADX 指标与成交标记观察，便于确认信号确实在 +DI 或 -DI 上穿 ADX 时触发。

## 指标
- `AverageDirectionalIndex`（提供 ADX 以及 +DI、-DI 分量）。
