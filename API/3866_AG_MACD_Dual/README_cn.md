# AG 双重 MACD 策略

## 概述
本策略是 MetaTrader 4 专家顾问 **AG.mq4** 的 StockSharp 移植版。机器人同时计算两组不同参数的 MACD 指标：第一组作为入场触发器，第二组放大后的 MACD 用来过滤趋势并控制离场。策略仅在 K 线收盘后评估信号，从而还原原始 EA 的行为。

## 交易逻辑
- **指标设置**
  - 主 MACD：快速 EMA = `FastEmaLength`、慢速 EMA = `SlowEmaLength`、信号 SMA = `SignalSmaLength`。
  - 次 MACD：快速 EMA = `SlowEmaLength * 2`、慢速 EMA = `FastEmaLength * 2`、信号 SMA = `SignalSmaLength * 2`。
- **做多条件**
  - 主 MACD 主线位于信号线之上。
  - 主 MACD 信号线小于 0。
  - 次 MACD 主线位于信号线之上。
  - 次 MACD 信号线小于 0。
- **做空条件**
  - 主 MACD 主线位于信号线之下。
  - 主 MACD 信号线大于 0。
  - 次 MACD 主线位于信号线之下。
  - 次 MACD 信号线大于 0。
- **离场规则**
  - 当次 MACD 变为看空且主 MACD 信号线仍大于 0 时平掉多头。
  - 当次 MACD 变为看多且主 MACD 信号线仍小于 0 时平掉空头。
- 仅处理已完成的 K 线，忽略未收盘数据以避免重绘。

## 仓位管理
- 所有交易均使用 `OrderVolume` 指定的固定手数，以市价单执行。
- `MaxOpenOrders` 对应原始输入 `ORDER`，限制活动订单与持仓的总数；设为 `0` 即取消限制。
- 在启动时调用 `StartProtection()`，让 StockSharp 风控模块监控敞口。

## 参数
| 名称 | 说明 |
| --- | --- |
| `OrderVolume` | 开仓和加仓使用的基础手数。 |
| `FastEmaLength` | 主 MACD 的快速 EMA 周期。 |
| `SlowEmaLength` | 主 MACD 的慢速 EMA 周期。 |
| `SignalSmaLength` | 两个 MACD 共用的信号线平滑周期。 |
| `MaxOpenOrders` | 活动订单和持仓的最大数量，`0` 表示不限。 |
| `CandleType` | 构建指标所使用的 K 线类型或周期。 |

## 备注
- 次 MACD 的快、慢周期保持与原始 EA 相同的顺序，即使快速周期大于慢速周期也不会调整，以确保行为一致。
- 策略不会挂出挂单，信号触发后立即以市价进出场。
- 未额外添加止损或止盈，完全依赖信号反转离场，与原版一致。
