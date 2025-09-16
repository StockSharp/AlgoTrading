# SimpleTrade 策略

## 概述
- 基于 MetaTrader 4 专家顾问 **SimpleTrade.mq4**（亦称 “neroTrade”）的 StockSharp 移植版本。
- 通过 `CandleType` 参数指定交易时间框架，适用于任何单品种行情。
- 每根新 K 线开盘都会重新评估方向，账户中始终只保留一笔仓位。

## 交易逻辑
1. 当新的 K 线进入 `Active` 状态时，策略会读取该 K 线的开盘价，并与 `LookbackBars` 根之前那根 K 线的开盘价进行比较。
2. 如果新的开盘价 **高于** 参考开盘价，则先平掉所有仓位，再按 `TradeVolume` 手数市价做多。
3. 如果新的开盘价 **小于或等于** 参考开盘价，则平掉所有仓位，并按同样的手数市价做空。
4. `StopLossPoints` 对应原始 EA 的 `stop` 参数。只要证券元数据里同时提供 `PriceStep` 和该参数，策略就会把点值换算成绝对价格并传递给 `StartProtection`，由 StockSharp 自动维护止损单。
5. 历史开盘价通过高阶蜡烛订阅接口累积：已完成的蜡烛用于填充缓存，活跃蜡烛在每根柱子的第一笔更新时触发决策。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `TradeVolume` | 下单手数（lots），必须为正数。 | `1` |
| `StopLossPoints` | 止损距离，单位为品种点值。设为 `0` 可禁用自动止损。 | `120` |
| `LookbackBars` | 用于比较开盘价的历史柱子数量。默认值 `3` 等价于原代码中的 `Open[0]` 与 `Open[3]`。 | `3` |
| `CandleType` | 请求蜡烛的 `DataType`，决定信号触发的时间框架。 | `1 小时周期` |

## 实现细节
- 采用高阶 `SubscribeCandles(...).Bind(...)` 工作流，既支持回放也能及时响应实时行情。
- 在 `OnStarted` 中调用 `StartProtection`，请确保所选证券提供 `PriceStep`；否则无法换算出绝对止损价位。
- 策略所有交易均在每根新柱开盘时使用市价单完成，不再提供额外的滑点参数。
- 开盘价缓存只保留 `LookbackBars + 5` 条记录，以限制内存占用。
- 本目录仅包含 C# 版本，暂无 Python 实现。

## 文件结构
```
4002_SimpleTrade/
├── CS/
│   └── SimpleTradeStrategy.cs
├── README.md
├── README_cn.md
└── README_ru.md
```
