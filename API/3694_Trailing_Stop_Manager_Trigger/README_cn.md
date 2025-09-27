# Trailing Stop Manager 策略

## 概述
**Trailing Stop Manager 策略** 是对 MetaTrader 专家顾问 `Trailing Sl.mq5` 的 StockSharp 移植版本。原始 EA 并不会主动开仓，
而是监听具有指定 *magic number* 的既有持仓，当行情向有利方向推进时上调止损位。本 C# 实现利用 StockSharp 的高层
API 重现这一逻辑，为任何受 StockSharp 支持的标的提供透明的跟踪止损管理。

## 跟踪逻辑
1. 订阅盘口，持续获取最新的买一与卖一报价。
2. 判断当前净头寸的方向（多头或空头）。
3. 使用相应的市场一档报价计算浮动盈亏（多头使用买价，空头使用卖价）。
4. 当浮盈超过 `TriggerPoints`（通过 `PriceStep` 转换为价格单位）时启动跟踪模式。
5. 在当前行情附近按照 `TrailingPoints` 的距离放置跟踪止损线。
6. 仅在盈利扩大时向盈利方向移动止损，以持续锁定利润。
7. 一旦一档报价触及计算出的止损线，立即发送对冲方向的市价单平掉整笔仓位。

## 订单与风控
- 策略**不会**发送初始进场订单，只负责管理已经存在的头寸，无论该头寸来自手动交易还是其他策略。
- 通过 `BuyMarket`/`SellMarket` 完成平仓，对应 MetaTrader 中的 `PositionModify` 操作。
- 跟踪距离会根据标的的 `PriceStep` 自动换算，保留 EA 中以点为单位的配置体验。
- 头寸平仓后，内部状态会被重置，新建头寸会重新按照触发条件开始管理。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `TrailingPoints` | `int` | `1000` | 当前价格与跟踪止损之间的距离，单位为价格步长。 |
| `TriggerPoints` | `int` | `1500` | 启动跟踪所需的最小浮盈，单位为价格步长。 |

## 使用建议
- 将策略附加到需要被管理的证券上后，它会立即开始跟踪当前的净头寸。
- 请将策略的初始 `Volume` 设置为与现有仓位数量一致。StockSharp 采用净头寸模式，触发止损后会一次性平掉全部仓位。
- 如果交易品种的价格步长较大，可适当上调 `TrailingPoints` 和 `TriggerPoints` 以避免过早出场。
- 策略的全部状态都保存在 StockSharp 内部，可与任何依赖 StockSharp 执行订单的人工或自动系统搭配使用。

## 与原版 MetaTrader EA 的差异
- MetaTrader 根据 *magic number* 区分不同订单；StockSharp 采用单一净头寸模型，因此不再需要逐票筛选。
- 原 EA 中的 `Setloss`、`TakeProfit` 与 `Lots` 参数未被使用，本移植版本也不再提供，以突出跟踪止损的核心功能。
- `PositionModify` 被替换为直接的市价平仓，这是 StockSharp 净值账户中最稳定的做法。
