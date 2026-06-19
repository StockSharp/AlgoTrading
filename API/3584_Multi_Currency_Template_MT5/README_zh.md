# MultiCurrency Template MT5 策略

## 概述

**MultiCurrency Template MT5 策略** 复刻了同名 MetaTrader 专家顾问的行为。策略在日线级别寻找由两根 K 线组成的形态，并允许同时管理多个品种的持仓篮子。只有当前一根日线的多空走势满足条件时才会开出初始仓位，随后在更快的控制周期上执行风控管理。当行情逆势运行达到预设的 MetaTrader 点数时，马丁格尔模块会补加仓位；退出逻辑同时结合了固定止盈、均价止盈以及可选的移动止损。

移植到 StockSharp 后仍然保留了多品种管理能力，用户可以通过逗号分隔的列表指定多个交易标的。每个标的都拥有独立的上下文、持仓篮子以及资金管理数据；当 `TradeMultipair` 关闭时，策略仅交易实例所附加的 `Security`。

## 信号生成

* 订阅 `SignalCandleType`（默认日线）并缓存最近两根收盘完成的 K 线。
* 当最新收盘价低于上一根开盘价且上一根 K 线收阳时触发**做多**信号。
* 当最新收盘价高于上一根开盘价且上一根 K 线收阴时触发**做空**信号。
* 任意时刻仅允许一个方向的持仓；当前篮子未平仓之前忽略新的信号。

## 下单逻辑

* 按 `Lots` 参数设置的手数以市价成交开仓。
* 开启 `NewBarTrade` 后，策略会等待 `TradeCandleType` 周期内形成一根新的完成 K 线后才允许开仓。该标志在首次决策时被消费，用于模拟原始 EA 的“只在新柱开单”行为。
* 初始止损、止盈均使用 MetaTrader 的“点”/“点值”换算，以保持与原策略一致的距离。
* 启用 `EnableMartingale` 时，当价格与当前篮子最佳入场价的偏离达到 `StepPoints` 时会加仓，手数按 `NextLotMultiplier` 的幂次递增。

## 仓位管理

* `EnableTakeProfitAverage` 控制止盈方式：
  * 关闭时，止盈始终保持在 `TakeProfitPips` 对应的固定距离。
  * 开启且篮子中至少有两笔订单时，止盈移动到盈亏平衡价再加上 `TakeProfitOffsetPoints`。
* 每次成交后都会重新计算止损，以反映当前篮子中最不利的价格。
* 当仅剩一笔订单时启用移动止损：价格突破 `TrailingStopPoints + TrailingStepPoints` 后先移动到盈亏平衡价以上 `TrailingStopPoints`，之后继续按相同距离跟随。
* 风控触发时会以市价一次性平掉当前方向的全部篮子。

## 参数

| 参数 | 说明 |
| --- | --- |
| `Lots` | 每个篮子首笔订单的基础手数。 |
| `StopLossPips` | 初始止损距离（MetaTrader 点）。 |
| `TakeProfitPips` | 初始止盈距离（MetaTrader 点）。 |
| `TrailingStopPoints` | 仅剩一笔订单时的移动止损距离（MetaTrader 点）。 |
| `TrailingStepPoints` | 再次调整移动止损前需要的额外缓冲（点）。 |
| `SlippagePoints` | 为保持一致性保留的滑点输入，不影响实际成交。 |
| `NewBarTrade` | 是否启用按新柱过滤开仓。 |
| `TradeCandleType` | 用于检测新柱和执行风控的心跳周期。 |
| `TradeMultipair` | 是否启用多品种模式。 |
| `PairsToTrade` | 逗号分隔的其他证券代码，通过 `GetSecurity` 解析。 |
| `Commentary` | 订单备注，仅用于记录。 |
| `EnableMartingale` | 是否启动逆势加仓模块。 |
| `NextLotMultiplier` | 每次加仓手数相对于上一笔的乘数。 |
| `StepPoints` | 触发下一次加仓的点数偏离。 |
| `EnableTakeProfitAverage` | 是否对多笔订单使用均价止盈。 |
| `TakeProfitOffsetPoints` | 启用均价止盈时在盈亏平衡价基础上额外增加的点数。 |
| `SignalCandleType` | 构建两根 K 线形态使用的周期（默认日线）。 |

## 备注

* 策略全部采用市价单开平仓，MetaTrader 的保护单在内部模拟实现。
* `PairsToTrade` 中的代码必须能被连接的交易通道识别，未知代码会被忽略。
* 马丁格尔与移动止损均按品种独立运行，每个标的维护自己的篮子。
* `SlippagePoints` 仅作为占位参数，在 StockSharp 中不会影响实际成交。

