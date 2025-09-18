# 风险管理信息面板策略

## 概述
**风险管理信息面板策略** 复刻了 MetaTrader 5 专家顾问 *RiskManager_with_InfoPanel_and_Support* 的仪表盘功能。原始 EA 不会下单，而是持续计算账户风险指标，在图表上显示一块信息面板并附带客服按钮。StockSharp 版本保持相同的分析定位：根据设定的风险百分比计算推荐仓位规模，展示预期的止损/止盈价格，并监控日内亏损是否超出阈值。所有结果都会写入策略的注释 (`Strategy.Comment`)，因此在 Designer、Shell 或自定义界面中都能以面板形式展现。

该模块不会自动开平仓，它专注于风险可视化，可与人工交易或其他策略协同使用。

## 主要特性
- 按照原 EA 的公式，用账户权益、止损距离和最小变动价值计算推荐手数。
- 跟踪账户余额、权益、浮动盈亏及最近一次更新时间。
- 对指定的入场价给出止损/止盈价格、点差距离、风险收益比等指标。
- 按日累计已实现盈亏，并在亏损超过日度风险限制时给出警告。
- 支持可选的客服消息，使策略注释能够重现 MetaTrader 中的“Support”按钮文本。

## 参数
| 参数 | 默认值 | 说明 |
|------|--------|------|
| `RiskPercent` | `1` | 每笔交易占用的权益百分比。 |
| `EntryPrice` | `1.1000` | 计算止损与止盈时使用的参考入场价。 |
| `StopLossPercent` | `0.2` | 止损相对入场价的百分比偏移。 |
| `TakeProfitPercent` | `0.5` | 止盈相对入场价的百分比偏移。 |
| `MaxDailyRiskPercent` | `2` | 日度亏损阈值，当已实现盈亏低于 `-权益 × MaxDailyRiskPercent / 100` 时触发警告。 |
| `UpdateIntervalSeconds` | `10` | 刷新信息面板的时间间隔（秒）。 |
| `UseSupportMessage` | `true` | 是否在注释中附加客服提示。 |
| `SupportMessage` | `"Contact support for assistance!"` | 附加在统计信息后的文本，用于仿真原策略的客服按钮。 |

所有参数都可以在 Designer 属性面板中实时修改，或通过 `Strategy.Param` 编程设置。风险参数带有非负性校验，对应原 MQL 输入的限制条件。

## 使用方法
1. 将策略绑定到目标品种与组合。
2. 配置参考入场价、风险百分比以及可选的客服消息。
3. 启动策略。每隔 `UpdateIntervalSeconds` 秒注释会更新一次，输出与 MT5 信息面板类似的块状文本：
   ```
   Risk Manager for EURUSD@FX
   -----------------------------
   Account: DEMO-12345
   Balance: 10000.00
   Equity: 10025.34
   Floating PnL: 25.34
   Updated: 12:30

   Risk/Trade: 1.00%
   Entry Price: 1.10000
   Stop Loss: 1.09780 (0.20%)
   Take Profit: 1.10550 (0.50%)

   Distance (pips): 22.0
   Risk ($): 100.25
   Recommended Volume: 0.45
   Reward:Risk Ratio: 1.82

   Daily P/L: 35.00
   Daily Risk Limit: 200.51
   ```
4. 在界面中绑定 `Strategy.Comment` 或公开的 `RiskSnapshot` 字符串即可重现信息面板。当日亏损超过阈值时会自动追加 `*** DAILY RISK LIMIT EXCEEDED! Trading suspended.` 警告。

## 与原版的差异
- 不再创建图表对象，而是将面板内容写入 `Strategy.Comment`，从而保持跨平台兼容。
- 点值计算依赖 `Security.PriceStep` 与 `Security.StepPrice`，可同时适配期货、外汇与差价合约，无需硬编码点值。
- 日度已实现盈亏通过 `MyTrade.PnL` 事件累计，并会在跨日后首次更新时自动清零。
- 支持消息仅为文本形式，StockSharp 策略不直接管理图表控件，因此没有可点击按钮。

## 注意事项
- 请确保所选组合能提供 `Portfolio.CurrentValue` 和 `Portfolio.CurrentBalance`。若无法获取，这些统计将回退为 0，推荐手数也会变为 0。
- 本策略仅用于信息展示，不会调用 `Buy/Sell` 等下单方法，应与人工操作或其他自动化策略配合使用。
- 风险监控只在启动时初始化一次，符合 StockSharp 关于保护型模块的最佳实践。
