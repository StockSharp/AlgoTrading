# Pending tread 网格策略

## 概述
**Pending tread 网格策略** 是 MetaTrader 4 专家顾问 `Pending_tread.mq4` 的 StockSharp 版本。原始 EA 会在行情上方与下方持续维护两组挂单梯形，每一组都可以选择使用买单或卖单，并以点数控制间距。本移植完全使用 StockSharp 高阶 API 实现同样的逻辑，没有引入额外指标或自建数据集合。

## 交易逻辑
1. **买卖价驱动的维护** – 通过 `SubscribeLevel1` 订阅一级行情，缓存最新的买价与卖价。每当接收到新报价时（受可配置的节流限制），维护流程会检查当前挂单数量与目标网格是否一致。
2. **上方挂单梯形** – `AboveMarketSide` 决定在市场上方放置买入止损或卖出限价单。每一个阶梯相距 `PipStep` 点，并附带 `TakeProfitPips` 点的止盈。
3. **下方挂单梯形** – `BelowMarketSide` 控制在市场下方堆叠买入限价或卖出止损单，点距与止盈计算与上方梯形相同。
4. **止损距离保护** – `MinStopDistancePoints` 用来模拟 MetaTrader 的 `MODE_STOPLEVEL` 限制。如果挂单价格与对应的买价/卖价之间的距离小于限制，挂单会被跳过。
5. **节流机制** – `ThrottleSeconds` 复刻了原程序中防止 “TRADE_CONTEXT_BUSY” 的 5 秒节流。在该时间窗口内只会执行一次维护，即使行情频繁更新。

所有以点数表示的输入（`PipStep`、`TakeProfitPips`）都会根据品种的 `PriceStep` 与 `Decimals` 转换为绝对价格偏移。对于五位或三位报价会自动乘以十，以匹配 MetaTrader 的 “adjusted point” 处理方式。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `OrderVolume` | 0.01 | 每个挂单的下单数量，下单前会根据品种的最小步长进行修正。 |
| `PipStep` | 12 | 相邻挂单之间的点数间隔。 |
| `TakeProfitPips` | 10 | 每个挂单对应的止盈距离，单位为点。 |
| `OrdersPerSide` | 10 | 市场上方与下方各维护的最大挂单数量。 |
| `AboveMarketSide` | Buy | 市场上方使用的挂单类型。`Buy` 表示买入止损，`Sell` 表示卖出限价。 |
| `BelowMarketSide` | Sell | 市场下方使用的挂单类型。`Buy` 表示买入限价，`Sell` 表示卖出止损。 |
| `MinStopDistancePoints` | 0 | 买卖价与挂单价格之间允许的最小距离（点）。如有需要，可填写经纪商提供的 `MODE_STOPLEVEL`。 |
| `ThrottleSeconds` | 5 | 每次维护之间的冷却时间，单位为秒。 |
| `SlippagePoints` | 3 | 为与 MT4 输入保持一致而保留；在 StockSharp 中对挂单不起作用。 |

## 实现说明
- 仅使用 StockSharp 高阶接口（`SubscribeLevel1`、`BuyLimit`、`SellLimit`、`BuyStop`、`SellStop`）。
- 所有价格通过 `Security.ShrinkPrice` 归一化，确保满足交易所最小跳动要求。
- 下单数量会根据 `VolumeStep`、`MinVolume`、`MaxVolume` 自动调整。
- 日志信息使用 `AddInfoLog` / `AddWarningLog` 输出，保留原 EA 的详细提示风格。
- 根据任务要求，本目录未提供 Python 版本。

## 使用提示
1. 绑定好品种与投资组合后启动策略。收到首个一级行情后两组挂单会立即生成。
2. 调整 `OrdersPerSide` 时需注意风险，每增加一个阶梯就会在经纪商侧新增一张挂单。
3. 若要完全复刻原 EA，请保持 5 秒节流并设置 `MinStopDistancePoints` 为经纪商要求的止损距离。
4. StockSharp 采用净头寸模型，若上下两侧同时触发，成交会互相对冲，而不会形成 MT4 式的双向持仓。
