# Symbol Swap 策略

**Symbol Swap Strategy** 是 MetaTrader 5 工具 “Symbol Swap” 的 StockSharp 版本。原始的 MQL5 面板允许交易者输入任意品种，立即把图表切换到该符号，并显示一个包含时间、OHLC 价格、tick 成交量与点差的数据窗口。本次移植保持同样的职责，同时完全依赖 StockSharp 的高级订阅接口实现。

## 工作流程

1. 启动后策略会解析需要监控的标的。优先使用 `WatchedSecurityId`，如果为空则回退到启动器中配置的 `Strategy.Security`。
2. 通过 `SubscribeCandles(...)` 订阅所选 `CandleType` 的蜡烛。每当蜡烛收盘时都会提供 Open/High/Low/Close 和 tick 成交量，用来更新面板字段。
3. 使用 `SubscribeLevel1(...)` 订阅最优买卖报价。每次报价变化都会重新计算点差，与 MetaTrader 的数据窗口保持一致。
4. 生成的文本块可以写入策略日志（`OutputMode = Log`），也可以借助 `DrawText(...)` 叠加在图表上（`OutputMode = Chart`），从而复刻 MQL 浮动面板的视觉效果。
5. 运行过程中调用 `SwapSecurity("TICKER")` 会通过 `SecurityProvider.LookupById` 查找新的品种，并无缝重建蜡烛与 Level 1 订阅。

策略仅用于信息展示，不会提交任何订单。它既可作为行情面板单独使用，也可与其他交易系统同时运行。

## 参数

| 名称 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 构建 OHLC 与 tick 成交量所用的蜡烛类型。 | `TimeFrame(1 minute)` |
| `WatchedSecurityId` | 可选的标的代码。留空时使用 `Strategy.Security`。 | _空_ |
| `OutputMode` | 信息块的呈现方式：`Chart` 表示绘制在图表上，`Log` 表示写入日志。 | `Chart` |

## 公共方法

| 方法 | 说明 |
|------|------|
| `SwapSecurity(string securityId)` | 通过当前 `SecurityProvider` 查找给定代码，并立即将面板切换到该标的。每次调用都会先清理旧的订阅，再创建新的蜡烛和 Level 1 订阅。 |

## 使用提示

- 请确认连接器能够识别所需的代码，否则 `SecurityProvider.LookupById` 会抛出异常。
- 当 `OutputMode = Chart` 时，策略会自动创建图表区域、绘制对应的蜡烛，并叠加数据文本。若选择 `Log`，则只输出文本日志。
- Tick 成交量直接取自蜡烛的 `TotalVolume` 字段，与 MetaTrader 的处理方式相同。
- 只有同时获得 best bid 和 best ask 时才会显示点差，否则字段显示 `n/a`。

## 移植说明

- MQL 中的定时器循环被 StockSharp 的订阅机制取代：蜡烛在收盘时触发一次，Level 1 则在每次报价变动时触发。
- 九个标签被组合成一个多行文本，顺序保持与原程序一致：Time、Period、Symbol、Close、Open、High、Low、Tick Volume、Spread。
- 不再需要手动把符号加入 Market Watch，策略直接通过 `SecurityProvider` 查找标的。
- 整个实现只使用高级 API（`SubscribeCandles`、`SubscribeLevel1`、`DrawText`、`AddInfo`），符合仓库的编码要求。

