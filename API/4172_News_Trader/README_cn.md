# News Trader 策略

该策略复刻了原始的 **NewsTrader.mq4** 脚本：在重要新闻发布前同时布设多空方向的突破挂单。一旦进入预设的倒计时（默认提前 10 分钟），策略会发送买入止损和卖出止损委托，并在任一方向成交后立即附加止损/止盈保护。

## 核心逻辑

- 订阅 1 分钟 K 线（可自定义）仅用于追踪时间推进。
- 将激活时间计算为 `新闻时间 - LeadMinutes`，等待第一根开盘时间不早于该时刻且已经收盘的 K 线。
- 通过 `BiasPips * Security.PriceStep` 将偏移量转换为价格，与 MQL4 中的 `bias * Point` 保持一致，在现价上下分别放置突破挂单。
- 当其中一个挂单成交时，取消另一侧挂单，并按照设定的止损/止盈距离提交对应的保护性订单。
- 止损或止盈成交后取消剩余的保护单并清空仓位。
- 在 `OnStarted` 中调用 `StartProtection()`，以便与 StockSharp 的组合级风险控制联动。

## 参数

| 名称 | 说明 | 默认值 |
| --- | --- | --- |
| `TradeVolume` | 每个挂单的交易数量。 | `1` |
| `StopLossPips` | 止损距离（单位：pip，0 表示不下止损单）。 | `10` |
| `TakeProfitPips` | 止盈距离（单位：pip，0 表示不下止盈单）。 | `10` |
| `BiasPips` | 挂单相对参考价的偏移距离。 | `20` |
| `LeadMinutes` | 新闻发布前提前多少分钟布单。 | `10` |
| `NewsYear`, `NewsMonth`, `NewsDay`, `NewsHour`, `NewsMinute` | 新闻的日期时间（以交易服务器时间为准）。 | `2010`, `3`, `8`, `1`, `30` |
| `CandleType` | 用于计时的 K 线类型。 | `1 Minute` |

## 实现细节

- `OnStarted` 中将策略的 `Volume` 设为 `TradeVolume`，确保 `BuyStop`/`SellStop` 等辅助方法使用正确的下单手数。
- 如果标的没有提供 `PriceStep`，策略会抛出异常，因为无法把 pip 转换成价格差。
- 采用当前分钟 K 线的收盘价作为最新报价的近似值，与原脚本中使用最新 `Bid/Ask` 的做法一致。
- 策略只在预设新闻事件前布单一次，事件过后不会自动重新挂单。
- 当 `StopLossPips` 或 `TakeProfitPips` 为 0 时，对应的保护订单会被跳过，方便结合人工风控。

## 文件

- `CS/NewsTraderStrategy.cs` — C# 实现。

按需求未提供 Python 版本。
