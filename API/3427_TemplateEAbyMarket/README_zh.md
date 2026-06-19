# TemplateEAbyMarket 策略

## 概览
TemplateEAbyMarket 是 MetaTrader 4 智能交易程序 *TemplateEAbyMarket.mq4* 在 StockSharp 平台上的直接移植版本。策略依靠 MACD 指标捕捉动量变化：当 MACD 主线与信号线发生交叉，并且两条线同时位于零轴同一侧时，就按照交叉方向提交市价单。离场完全交由保护模块处理，通过 `StartProtection` 一次性设置的止盈止损来控制风险。

该移植版本保持了原始 MQL 代码的行为：策略只负责开仓，并不会主动反向平仓。仓位建立后，由保护单或人工操作负责后续管理。

## 交易逻辑
1. 订阅用户选择的蜡烛类型（默认 15 分钟）。
2. 在每根收盘蜡烛上计算 MACD，默认参数为 12/26/9。
3. 监控 MACD 主线与信号线的相对位置以判定交叉：
   - **做多条件：** 上一根蜡烛主线位于信号线之下，本根蜡烛收盘后主线位于信号线之上，且两条线均大于零。若当前持仓绝对值小于 `MaxOrders * OrderVolume`，则提交 `OrderVolume` 的买入市价单。
   - **做空条件：** 上一根蜡烛主线位于信号线之上，本根蜡烛收盘后主线位于信号线之下，且两条线均小于零。在同样的仓位限制下提交卖出市价单。
4. 启动时激活一次止盈(`takeProfit`)与止损(`stopLoss`)。策略不会自动反手，风控由保护模块或人工完成。

## 参数
| 名称 | 说明 |
|------|------|
| `MacdFastPeriod` | MACD 快速 EMA 的周期。 |
| `MacdSlowPeriod` | MACD 慢速 EMA 的周期。 |
| `MacdSignalPeriod` | MACD 信号线 EMA 的周期。 |
| `CandleType` | 用于计算指标的蜡烛类型（时间框架）。 |
| `OrderVolume` | 每次下单的数量（手数或合约数）。 |
| `MaxOrders` | 允许的最大同时持仓量，以 `OrderVolume` 的倍数表示。下单前会检查 `abs(Position) < MaxOrders * OrderVolume`。 |
| `TakeProfitPoints` | 止盈距离（价格点）。取值为 `0` 时禁用止盈。 |
| `StopLossPoints` | 止损距离（价格点）。取值为 `0` 时禁用止损。 |

## 说明
- MQL 版本中的滑点与 magic number 设置未被移植，因为在 StockSharp 中由其他机制处理。
- 请确保连接器提供正确的最小变动价位信息，`StartProtection` 会以该单位解释止盈止损距离。
- 策略保持模板化设计，不处理部分成交，也不会在 `MaxOrders` 限制之外进行加仓。
