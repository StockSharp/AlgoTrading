# ReadNewsByWebRequest 策略

## 概述
**ReadNewsByWebRequestStrategy** 是 MetaTrader 专家顾问 “ReadNewsByWebRequest.mq4” 的 StockSharp 版本。
策略会不断下载 Forex Factory 的每周经济日历，并在每个高影响力事件发布前构建一个双向突破（straddle）。
在距离公布时间指定的分钟数时，系统会同时挂出买入止损和卖出止损订单，以捕捉发布后的瞬间波动。

## 外部数据
* **数据源**：`https://nfs.faireconomy.media/ff_calendar_thisweek.xml`（Forex Factory XML 日历）。
* **刷新频率**：可配置（默认 1 分钟）。策略通过 HTTP GET 请求获取 XML 并直接解析内容。
* **过滤规则**：只保留标记为 `High` 的事件，并且发布时间必须晚于当前时间。
* **时区假设**：数据源使用 GMT/UTC。所有时间比较都采用报单服务器时间 `Level1ChangeMessage.ServerTime`。
  标记为 *All Day*、*Tentative*、*Holiday* 等没有明确时间的事件会被忽略。

## 运行流程
1. 启动策略并绑定到外汇品种。策略会使用证券的价格步长与数量步长，将“点数”转换为真实价格和数量。
2. 启动时立即下载日历，并按照 `RefreshMinutes` 设置启动定时器定期刷新。
3. 订阅一级行情（Level1），用于更新最新买价与卖价。
4. 当当前时间进入发布前 `LeadMinutes` 分钟的窗口时，在市场价附近挂出一组买/卖止损单。
5. 若设置了 `StopLossPoints` 或 `TakeProfitPoints`，策略会调用 `StartProtection` 自动创建止损与止盈保护单。
6. 到达 `PendingExpirationMinutes` 或事件的发布时间/失效时间后，尚未成交的挂单会被撤销。
7. 当与事件相关的所有订单都处于非激活状态时，该事件会从内部列表中移除。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `Volume` | `0.01` | 下单手数。会根据交易品种的最小/最大数量以及数量步长自动调整。 |
| `StopLossPoints` | `300` | 止损距离（点）。设为 `0` 表示不使用止损。 |
| `TakeProfitPoints` | `900` | 止盈距离（点）。设为 `0` 表示不使用止盈。 |
| `BuyDistancePoints` | `200` | 买入止损相对当前卖价（Ask）的偏移点数。 |
| `SellDistancePoints` | `200` | 卖出止损相对当前买价（Bid）的偏移点数。 |
| `LeadMinutes` | `5` | 在新闻公布前多少分钟挂出突破订单。 |
| `PendingExpirationMinutes` | `10` | 挂单有效期（分钟）。`0` 表示无限期。 |
| `RefreshMinutes` | `1` | 重新下载 Forex Factory 日历的时间间隔。 |
| `ShowNewsLog` | `false` | 若为 `true`，每次刷新后会在日志中显示当前跟踪的高影响事件数量。 |

## 订单管理
* 只有在策略在线并允许交易、且证券拥有有效的价格/数量步长信息时才会发单。
* 买入止损价格为 `Ask + BuyDistancePoints * PriceStep`，卖出止损价格为 `Bid - SellDistancePoints * PriceStep`。
* 当四舍五入后的下单数量为零时，策略会跳过挂单并记录警告信息。
* 过期的挂单通过 `CancelOrder` 撤销；成交后的仓位依靠 `StartProtection` 的保护单来管理风险。

## 风险控制
* 只要设定了止损或止盈点数，`StartProtection` 就会自动启用。
* 每个新闻事件只会生成一组对称的突破挂单，不使用加仓、摊平或马丁策略。
* 发布窗口结束后，事件会被标记为完成，从而阻止重复下单。

## 注意事项与限制
* 需要能够访问 Forex Factory 网站。下载失败会在日志中给出警告。
* 策略忽略中低影响力新闻以及没有精确时间的条目。
* 若数据源提供的时间与券商时区不同，可以调整 `LeadMinutes` 以提前或推迟挂单。
* 建议使用价格步长 (`PriceStep`)、小数位 (`Decimals`)、数量步长 (`VolumeStep`) 以及最小/最大数量信息完整的证券。

## 转换说明
* MQL 中 60 秒的 `EventSetTimer` 被映射到 StockSharp 的 `Timer.Start`，间隔可通过 `RefreshMinutes` 调整。
* 原始脚本使用字符串搜索解析 XML，这里改用 `XDocument` 进行结构化解析。
* MQL 中的 `CheckVolumeValue` 校验被替换为 `RoundVolume`，以适配 StockSharp 的交易品种约束。
* 原策略将新闻信息显示在图表左上角；在移植版中可通过 `ShowNewsLog` 控制是否写入日志。
* 挂单到期时间通过 `PendingExpirationMinutes` 参数复现（默认 10 分钟，与 MQL 版本一致）。
