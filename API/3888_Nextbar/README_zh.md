# Nextbar 策略

## 概述
**Nextbar** 策略是 MetaTrader 4 专家顾问 `nextbar.mq4` 的完整移植版本。原始 EA 会比较最新收盘价与若干根之前的收盘价，当价格单向移动超过阈值时，根据方向参数选择顺势或逆势入场。所有头寸都设置对称的止盈/止损，并在持仓达到固定的根数后强制平仓。

在 StockSharp 中我们使用高级策略 API 重建了同样的流程，只处理已经结束的 K 线，使得计算结果与 MT4 的“每根收盘时执行”逻辑完全一致。

## 原版 MQL 规则
* **动量距离**：比较 `Close[1]` 与 `Close[bars2check+1]`，若差值达到 `minbar * Point` 即视为有效信号。
* **方向参数**：`direction = 1` 表示顺势交易（上涨后买入、下跌后卖出），`direction = 2` 表示逆势交易（下跌后买入、上涨后卖出）。
* **入场限制**：同一时间只允许一笔持仓，新的交易只会在信号形成后的下一根 K 线开盘时发送。
* **离场规则**：多单在收盘价达到入场价上方的获利距离或下方的止损距离时平仓；空单条件相反。如果两者都未触发，则在持仓达到 `bars2hold` 根完成的 K 线后强制平仓。

## StockSharp 实现要点
* 通过 `SubscribeCandles()` + `Bind` 订阅指定时间框架的已完成 K 线。
* 使用一个短期的收盘价缓冲区获取 `bars2check + 1` 对应的历史收盘价，与 MQL 的索引完全一致。
* 所有以“点”为单位的参数都乘以 `Security.PriceStep`，等同于 MetaTrader 的 `Point` 常量。
* 市价单使用策略的 `Volume` 作为下单手数，`Direction` 参数决定顺势 (`Follow`) 还是逆势 (`Reverse`) 入场。
* 止盈、止损以及持仓根数检查都只在每根已完成 K 线上执行一次，与原始脚本保持同步。

## 参数
| 参数 | 说明 | 默认值 | 备注 |
|------|------|--------|------|
| `CandleType` | 计算信号所使用的时间框架。 | 1 小时 | 需保证所选证券提供该类型的 K 线。 |
| `BarsToCheck` | 最新收盘价与参考收盘价之间的完成 K 线数量。 | 8 | 对应 EA 中的 `bars2check`。 |
| `BarsToHold` | 持仓允许经过的最大完成 K 线数量。 | 10 | 等同于 `bars2hold`，在达到该数值时强制平仓。 |
| `MinMovePoints` | 两个收盘价之间的最小距离（点）。 | 77 | 对应 `minbar`，会乘以 `Security.PriceStep`。 |
| `TakeProfitPoints` | 止盈距离（点）。 | 115 | 对应 `profit` 参数，置零可关闭。 |
| `StopLossPoints` | 止损距离（点）。 | 115 | 对应 `loss` 参数，置零可关闭。 |
| `Direction` | 交易模式：`Follow` 顺势，`Reverse` 逆势。 | `Follow` | 与 `direction` 输入一致（`1`=顺势，`2`=逆势）。 |
| `Volume` | 市价单的下单手数。 | `Strategy.Volume` | 通过策略属性配置，等同 MT4 单笔订单的手数。 |

## 运行流程
1. 等待一根 K 线收盘并记录其收盘价。
2. 读取 `BarsToCheck` 根之前的收盘价并计算差值。
3. 若绝对差值小于 `MinMovePoints * PriceStep`，则忽略该信号。
4. 否则：
   * **Follow** 模式：上涨后做多，下跌后做空。
   * **Reverse** 模式：下跌后做多，上涨后做空。
5. 在持仓期间的每根已完成 K 线：
   * 多单：收盘价高于入场价 `TakeProfitPoints` 点或低于 `StopLossPoints` 点时平仓。
   * 空单：收盘价低于入场价 `TakeProfitPoints` 点或高于 `StopLossPoints` 点时平仓。
   * 当持仓经过 `BarsToHold` 根已完成 K 线后，立即平仓。

## 使用提示
* 点值转换依赖 `Security.PriceStep`（以及必要时的 `StepPrice`、成交量步长等元数据），请在运行前确保证券信息完整。
* 策略一次只管理一笔仓位，请根据实际需要设置 `Volume` 以匹配 MT4 中单笔订单的手数。
* 所有决策都基于已收盘的 K 线，因此需要使用能够提供完整 K 线的历史和实时数据源。
