# 多货币模板策略

## 概述
**多货币模板策略** 来自 MetaTrader 4 专家顾问 *Multi Currency Template v4* 的移植版本。策略在 StockSharp 高级 API 中复现了原始 EA 的 EMA 金叉/死叉入场逻辑、马丁格尔加仓、以点值计算的止损/止盈以及跟踪止损管理。默认使用 5 分钟K线，可通过参数修改。

## 交易逻辑
- 在所选周期的每根完结K线上计算两条指数移动平均线（EMA20 与 EMA50）。
- 当快线 EMA20 上穿慢线 EMA50 时触发买入信号；当快线下穿慢线时触发卖出信号。
- `Order Method` 参数决定策略是否同时交易多空，或仅限单方向操作。
- 策略保持单一净持仓。当出现反向信号时，会在开仓前平掉相反方向的仓位。

## 仓位管理
- **止损/止盈** – 输入值以 MetaTrader 点（pip）表示，并根据标的的最小价格步长转换为真实价格距离，兼容 4 位与 5 位外汇报价。
- **跟踪止损** – 当浮盈达到 `Trailing Stop (pts)`（以点表示）时启动，并在价格每增加 `Trailing Step (pts)` 后上移止损。
- **马丁格尔加仓** – 启用后，当价格每逆势运行 `Step (pts)` 时追加市价单。每次加仓的手数乘以 `Lot Multiplier`，直到仓位被平仓。
- **平均止盈** – 当存在两笔及以上加仓时，可选使用加权平均持仓价格加上 `Average TP Offset (pts)` 来模拟原版 EA 的“TP 平均”逻辑。

## 参数
| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| Order Method | 交易方向（买卖双向 / 仅做多 / 仅做空）。 | Buy & Sell |
| Volume (lots) | 初始市价单手数。 | 0.01 |
| Stop Loss (pips) | 以 MetaTrader 点表示的止损距离。 | 50 |
| Take Profit (pips) | 以 MetaTrader 点表示的止盈距离。 | 100 |
| Trailing Stop (pts) | 启动跟踪止损所需的点数。 | 15 |
| Trailing Step (pts) | 每次上移跟踪止损所需的额外点数。 | 5 |
| Enable Martingale | 是否启用逆势加仓。 | true |
| Lot Multiplier | 每次加仓使用的手数倍数。 | 1.2 |
| Step (pts) | 相邻加仓之间的点数间隔。 | 150 |
| Average Take Profit | 多单/空单加仓时是否使用平均止盈。 | true |
| Average TP Offset (pts) | 平均止盈价格相对加权价格的偏移点数。 | 20 |
| Candle Type | 指标所用的K线类型（时间框架）。 | 5分钟K线 |

## 与原 EA 的差异
- StockSharp 使用净持仓模型，而原 EA 逐票管理订单。马丁格尔加仓通过增加净仓位实现，而不是为每个订单设置独立目标价。
- 原 EA 支持在同一实例内循环多个交易品种；在 StockSharp 中需要为每个标的启动单独的策略实例。
- 原有的资金充足性检查（`CheckMoneyForTrade`, `CheckVolumeValue`）与经纪商限制由 StockSharp 的订单校验机制替代。

## 使用提示
1. 确认标的的价格步长与小数位设置正确，以便 pip 转换准确。
2. 目前跟踪止损与马丁格尔逻辑在每根K线收盘时执行，如需更高频率，可额外订阅盘口或逐笔成交并调用相应管理方法。
3. 策略采用市价单，具体滑点由经纪商或模拟器环境决定。
