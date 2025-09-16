# Type Pending Order Triggered 策略
[English](README.md) | [Русский](README_ru.md)

该工具策略复现了 MetaTrader 脚本 *TypePendingOrderTriggered.mq5* 的功能。本策略不会主动下单，只会监听自身成交，
并说明是哪一种挂单类型（Buy Limit、Sell Limit、Buy Stop 或 Sell Stop）触发了成交。

## 概述

- **目标**：在同时挂出多种订单时，明确是哪一种挂单导致了成交。
- **适用标的**：适用于 StockSharp 支持的任何证券，信息直接读取自 `MyTrade.Order`。
- **覆盖的订单类型**：
  - Buy Limit 与 Sell Limit（`OrderTypes.Limit` + 订单方向）。
  - Buy Stop 与 Sell Stop（`OrderTypes.Conditional` + 订单方向）。
  - 其他类型会触发警告，表示成交来自非挂单订单。
- **输出**：通过 `AddInfoLog` 打印信息，可在策略日志、终端输出或任何日志接收器中看到。

## 运行细节

1. 策略启动后仅等待自身成交，不需要订阅行情数据。
2. 每个 `MyTrade` 只处理一次，内部集合 `_reportedOrders` 防止同一订单多次提示（例如部分成交）。
3. 优先使用交易所分配的 `Id`，否则退回 `TransactionId` 作为识别号。
4. 根据订单方向 (`Sides.Buy`/`Sides.Sell`) 与 `Order.Type` 组合出文本，并输出与原版相同的英文提示：
   *"The pending order {ticket} is found! Type of order is ..."*。
5. 如果订单并非挂单（如市价单），则记录一条警告，与原脚本行为一致。
6. 当成交中缺少订单引用时也会给出警告，以方便排查。

## 实用建议

- 将本策略与真正负责下单的手动/自动策略一起运行，它会在每个订单的首次成交时给出分类结果。
- 由于策略不会调用 `BuyMarket`/`SellMarket`，可以安全地接入现有连接器而无需担心误下单。
- 输出日志在分析成交报告或从 MetaTrader 迁移到 StockSharp 时非常有帮助，可确认经纪商对挂单的处理方式。
- 重置或停止策略会清空已记录的订单编号，重新启动后会再次报告新的成交。

## 参数

本移植版没有新增参数，全部行为固定，以最大程度贴合原始 MQL 逻辑。

## 迁移说明

- MetaTrader 中依赖 `HistoryOrderSelect` 循环，在 StockSharp 中不再需要，因为 `MyTrade` 直接携带 `Order` 引用。
- 使用 StockSharp 的日志函数（`AddInfoLog`/`AddWarningLog`）替代原来的 `Print`。建议将其接入惯用的日志系统以便观察。
