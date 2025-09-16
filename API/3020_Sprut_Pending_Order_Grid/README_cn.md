# Sprut Pending Order Grid 策略

## 概述
**Sprut Pending Order Grid Strategy** 将 MetaTrader 5 智能交易系统 *Sprut (barabashkakvn's edition)* 移植到 StockSharp 的高层策略框架中。策略会在当前价格附近构建可配置的买卖挂单网格，管理每一笔订单的生命周期，按照原始公式放大手数，并在成交后通过 `BuyStop`、`SellStop`、`BuyLimit`、`SellLimit` 等高层方法自动挂出保护单。

与原始 MT5 版本保持一致的核心思想：

* 每个方向的第一笔挂单可以使用固定价格，也可以按照距离最佳买/卖价的点差自动计算；
* 网格按照独立的步长为止损单和限价单分层展开；
* 后续挂单的手数依据系数线性放大，与 MT5 中的实现完全一致；
* 每笔成交后会立即挂出对应的止损/止盈单，距离用点差表示；
* 设置整体的盈利/亏损阈值，触发后立即平仓并撤销所有挂单；
* 挂单可以按分钟设置有效期，到期自动撤单。

## 工作流程
1. **市场数据**：策略订阅盘口更新以获取最佳买卖价，同时订阅（默认 1 分钟）K 线以进行周期性维护，不需要任何指标。
2. **网格初始化**：在没有仓位且没有挂单时，计算每个方向的首单价格：
   * **Buy Stop**：最佳卖价 + `DeltaFirstBuyStop`（若 `FirstBuyStop` ≠ 0 则使用手动价格）。
   * **Buy Limit**：最佳买价 − `DeltaFirstBuyLimit`（若 `FirstBuyLimit` ≠ 0）。
   * **Sell Stop**：最佳买价 − `DeltaFirstSellStop`（若 `FirstSellStop` ≠ 0）。
   * **Sell Limit**：最佳卖价 + `DeltaFirstSellLimit`（若 `FirstSellLimit` ≠ 0）。
   点差通过 `Security.PriceStep` 转换为价格（默认 0.0001）。
3. **网格扩展**：对每个启用的方向生成 `CountOrders` 个挂单，间距分别为 `StepStop` 或 `StepLimit`。手数的计算方式：第 0 笔使用基础手数，其余为 `baseVolume * N * coefficient`（当系数 > 1）。生成的手数会根据 `VolumeStep`、`MinVolume`、`MaxVolume` 自动调整。
4. **到期处理**：若 `ExpirationMinutes` > 0，挂单会记录到期时间，超过时间后自动撤销。
5. **成交保护**：当挂单全部成交后，策略按照 `StopLoss`、`TakeProfit`（点差）自动挂出对应方向的止损和止盈单。距离为 0 时表示关闭该保护单。
6. **盈亏阈值**：每次数据更新都会重新计算总盈亏（已实现 + 未实现）。当总盈亏达到 `ProfitClose` 或跌破 `LossClose`（通常为负）时，会触发清仓流程：以市价平仓、撤销所有网格挂单以及保护单。仓位归零后策略会自动重新布置网格。
7. **持续维护**：每次循环都会清理已完成或过期的订单，在条件允许时重新挂出网格，并在清仓过程中暂停新单。

## 参数
| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CountOrders` | 每个启用方向的挂单数量。 | 5 |
| `FirstBuyStop`, `FirstBuyLimit`, `FirstSellStop`, `FirstSellLimit` | 首单固定价格（0 表示使用自动偏移）。 | 0 |
| `DeltaFirstBuyStop`, `DeltaFirstBuyLimit`, `DeltaFirstSellStop`, `DeltaFirstSellLimit` | 自动定价时相对于最优价的点差。 | 15 |
| `UseBuyStop`, `UseBuyLimit`, `UseSellStop`, `UseSellLimit` | 是否启用对应方向的网格。 | false |
| `StepStop`, `StepLimit` | 相邻止损/限价挂单之间的点差。 | 50 |
| `VolumeStop`, `VolumeLimit` | 第一笔止损/限价挂单的基础手数。 | 0.01 |
| `CoefficientStop`, `CoefficientLimit` | 后续挂单手数的放大系数（>1 时复制 MT5 行为）。 | 1.6 |
| `ProfitClose` | 触发清仓的总盈利阈值（账户货币）。 | 10 |
| `LossClose` | 触发清仓的总亏损阈值（账户货币，通常为负）。 | -100 |
| `ExpirationMinutes` | 挂单有效期（分钟，0 表示 GTC）。 | 60 |
| `StopLoss`, `TakeProfit` | 成交后挂出的止损/止盈点差（0 表示关闭）。 | 50 / 0 |
| `CandleType` | 用于维护逻辑的 K 线类型。 | 1 分钟 |

## 使用提示
* 至少开启一个方向 (`UseBuyStop`、`UseBuyLimit`、`UseSellStop`、`UseSellLimit`)，否则网格不会生成。
* 点差转换依赖 `PriceStep`，若交易品种具有特殊最小价格变动，请相应调整偏移量。
* `ProfitClose`/`LossClose` 采用 `Strategy.PnL`（已实现盈亏）与最新最优价计算出的浮动盈亏之和；请确认品种设置了 `PriceStep` 与 `StepPrice`。
* 保护单为独立的 StockSharp 订单，若手动平仓，仓位归零后剩余保护单会自动撤销。
* `CandleType` 仅决定维护频率，真正的下单行为仍在盘口更新时触发。

## 与 MT5 版本的差异
* 采用净额持仓模式，与 MT5 的 netting 逻辑一致。
* 保护单通过独立订单实现，而不是在挂单上直接填写 SL/TP。
* 手数归一化使用 `VolumeStep`、`MinVolume`、`MaxVolume`，在 CFD 或加密市场使用前请核对交易所参数。
* 清仓流程完全由盈亏阈值驱动，没有额外的“全部平仓”按钮。

## 快速开始
1. 将策略绑定到可以提供盘口和指定 `CandleType` K 线的连接器。
2. 根据策略需求配置方向开关和基础手数。
3. 如果需要保护单，请设置 `StopLoss`/`TakeProfit`（0 表示不挂）。
4. 根据账户规模调整 `ProfitClose` 与 `LossClose`。
5. 启动策略，等待收到首个盘口快照后网格会自动构建。

> **Python 版本** 未提供。本目录仅包含 C# 实现。
