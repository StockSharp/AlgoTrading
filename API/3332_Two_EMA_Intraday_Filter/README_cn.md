# 双均线日内过滤策略

## 概览
该策略基于 MetaTrader 专家顾问 **Expert_2EMA_ITF**，使用 StockSharp 高级 API 复刻实现。策略通过两条指数移动平均线（EMA）的金叉/死叉寻找方向，并利用平均真实波幅（ATR）计算限价单、止损和止盈位置，同时提供日内时间过滤器以避开不希望交易的分钟、小时或星期几。

## 交易逻辑
- 在所选蜡烛周期上计算快 EMA 与慢 EMA。
- 当快 EMA 从下向上穿越慢 EMA 时视为多头信号；从上向下穿越时视为空头信号。
- 多头信号出现时，根据 `LimitMultiplier * ATR` 的偏移量（并在可用时加上价差）在慢 EMA 下方挂买入限价单；空头信号则在慢 EMA 上方挂卖出限价单。
- 记录由 ATR 派生的止损与止盈价格，一旦限价单成交立即发送对应的保护性委托。
- 若挂单在 `ExpirationBars` 根 K 线后仍未成交则自动撤销。
- 仅在当前时间满足日内过滤条件（允许的分钟、小时、星期几，并且未被位掩码屏蔽）时才允许发送新信号。

## 指标
- **快 EMA**：用于快速响应价格变化。
- **慢 EMA**：用于定义趋势方向。
- **ATR**：衡量波动性并为入场、止损、止盈提供缩放因子。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 计算所用的蜡烛类型/周期。 | 30 分钟蜡烛 |
| `FastEmaPeriod` | 快 EMA 周期。 | 5 |
| `SlowEmaPeriod` | 慢 EMA 周期（必须大于快 EMA）。 | 30 |
| `AtrPeriod` | ATR 计算周期。 | 7 |
| `LimitMultiplier` | 入场价格的 ATR 偏移倍数。 | 1.2 |
| `StopLossMultiplier` | 止损价格的 ATR 倍数。 | 5 |
| `TakeProfitMultiplier` | 止盈价格的 ATR 倍数。 | 8 |
| `ExpirationBars` | 挂单最多保留的 K 线数量。 | 4 |
| `GoodMinuteOfHour` | 允许的分钟（-1 表示不限）。 | -1 |
| `BadMinutesMask` | 分钟屏蔽位掩码（第 *n* 位为 1 表示屏蔽第 *n* 分钟）。 | 0 |
| `GoodHourOfDay` | 允许的小时（-1 表示不限）。 | -1 |
| `BadHoursMask` | 小时屏蔽位掩码。 | 0 |
| `GoodDayOfWeek` | 允许的星期几（-1 表示不限，0=周日）。 | -1 |
| `BadDaysMask` | 星期几屏蔽位掩码（0=周日）。 | 0 |

## 委托管理
1. **入场委托**：依据 ATR 偏移量计算限价单价格，多头入场在可用时报价差补偿。
2. **过期控制**：记录挂单创建时的 K 线序号，超过 `ExpirationBars` 仍未成交则撤单。
3. **保护性委托**：当挂单成交后，取消旧的止损/止盈委托，并使用当时 ATR 计算得到的价格立即下达新的止损与止盈；当持仓归零时撤销保护单。

## 日内过滤细节
- **单值限制**：`GoodMinuteOfHour`、`GoodHourOfDay` 与 `GoodDayOfWeek` 分别限制允许的分钟、小时、星期几。
- **位掩码屏蔽**：`BadMinutesMask`、`BadHoursMask`、`BadDaysMask` 可一次性屏蔽多个时间段，例如 `BadMinutesMask = (1 << 0) | (1 << 30)` 会屏蔽每小时的第 0 与第 30 分钟。
- **综合判断**：只有在全部“允许”条件成立且没有掩码命中时，才允许发出新的交易信号。

## 与原始 EA 的差异
- 使用 StockSharp 的 `BuyLimit`/`SellLimit`/`SellStop`/`BuyStop` 等高阶方法管理挂单与保护单，而非 MetaTrader 的 Expert 库结构。
- 买入方向的价差补偿取自当前 `Security.BestBid` / `Security.BestAsk`，若无报价则偏移为 0。
- 时间过滤逻辑通过位掩码实现，代替了 MetaTrader 中的 `CSignalITF` 类。
- 订单填补后立刻下达保护性委托，行为与原始信号中返回的止损/止盈水平保持一致。

## 使用提示
- 启动策略前需设置 `Volume`，否则策略会记录警告并拒绝下单。
- 关键参数均开启了优化标记，可直接用于参数优化流程。
- 策略使用蜡烛的收盘时间来判定日内过滤条件。
