# 带信息面板和客服提示的风险管理器

## 概述

`RiskManagerWithInfoPanelAndSupportStrategy` 是从 MQL5 脚本 **RiskManager_with_InfoPanel_and_Support_noDLL** 移植到 StockSharp 的辅助策略。它不会自动下单，而是按照固定时间间隔在日志中输出详尽的账户快照，复刻原始指标面板的内容，并保留客服按钮的信息。

策略会计算每笔交易在控制风险前提下所需的仓位大小、评估盈亏比，并跟踪当日盈亏与可配置的日内亏损上限。当亏损超过阈值时，会在日志中发出停止交易的警告，与 MetaTrader 版本的行为一致。

## 工作流程

1. 启动策略时会检查是否已绑定投资组合，然后创建 `System.Threading.Timer`。定时器立即触发第一次执行，之后按照 `UpdateIntervalSeconds` 秒的周期运行。
2. 每次触发都会读取实时的组合数据（余额、净值、浮动盈亏），并结合设置的入场价、止损和止盈百分比。
3. 使用 `StringBuilder` 重新拼接信息面板文本并写入 Designer 日志。即使 StockSharp 不绘制图形，也能在日志中看到面板位置、字体和颜色设置。
4. 如果启用客服面板，则在输出中追加客服文字和 Bybit 推广链接，模拟 MQL 图表上可点击的按钮。
5. 通过记录每天第一次触发时的 PnL 值来追踪日内业绩。之后的 PnL 减去该基准即可得到日志中显示的当日盈亏。

策略本身不会提交任何订单，仅用于在开仓前持续观察账户状态和推荐手数，可与手动交易或其他 StockSharp 策略配合使用。

## 核心计算

- **单笔风险金额** = `PortfolioValue * RiskPercent / 100`。`PortfolioValue` 优先使用 `Portfolio.CurrentValue`，若经纪商不提供实时净值则退回到 `Portfolio.BeginValue`。
- **止损价** = `EntryPrice - EntryPrice * (StopLossPercent / 100)`。
- **止盈价** = `EntryPrice + EntryPrice * (TakeProfitPercent / 100)`。
- **推荐手数** = `RiskAmount / (StopDistanceInSteps * StepPrice)`，并向上取整到 `VolumeStep` 的整数倍，保证手数符合经纪商要求。
- **盈亏比** 使用止损和止盈转换成的价格步长距离计算。
- **当日盈亏** = `PnL - DailyPnLBase`。一旦进入新的一天或收到新的 PnL 更新，`DailyPnLBase` 会自动重置。
- **日内风险上限** = `Equity * MaxDailyRiskPercent / 100`。若当日盈亏跌破负值阈值，会在日志中追加警告。

## 参数

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `RiskPercent` | 1 | 单笔交易占账户资金的风险百分比。 |
| `EntryPrice` | 1.1000 | 计算风险时使用的参考入场价。 |
| `StopLossPercent` | 0.2 | 相对入场价的止损百分比。 |
| `TakeProfitPercent` | 0.5 | 相对入场价的止盈百分比。 |
| `MaxDailyRiskPercent` | 2 | 日内最大亏损占比，超过后在日志中发出警告。 |
| `UpdateIntervalSeconds` | 10 | 刷新报告的时间间隔（秒）。 |
| `InfoPanelXDistance` | 10 | 信息面板的虚拟 X 偏移（仅记录在日志）。 |
| `InfoPanelYDistance` | 10 | 信息面板的虚拟 Y 偏移。 |
| `InfoPanelWidth` | 350 | 信息面板的虚拟宽度。 |
| `InfoPanelHeight` | 300 | 信息面板的虚拟高度。 |
| `InfoPanelFontSize` | 12 | 面板字体大小（日志显示）。 |
| `InfoPanelFontName` | Arial | 面板字体（日志显示）。 |
| `InfoPanelFontColor` | White | 面板字体颜色（日志显示）。 |
| `InfoPanelBackColor` | DarkGray | 面板背景颜色（日志显示）。 |
| `UseSupportPanel` | true | 是否输出客服面板信息。 |
| `SupportPanelText` | Need trading support? Contact us! | 客服面板文字内容。 |
| `SupportPanelFontColor` | Red | 客服面板字体颜色。 |
| `SupportPanelFontSize` | 10 | 客服面板字体大小。 |
| `SupportPanelFontName` | Arial | 客服面板字体。 |
| `SupportPanelXDistance` | 10 | 客服按钮的虚拟 X 偏移。 |
| `SupportPanelYDistance` | 320 | 客服按钮的虚拟 Y 偏移。 |
| `SupportPanelXSize` | 250 | 客服按钮的虚拟宽度。 |
| `SupportPanelYSize` | 30 | 客服按钮的虚拟高度。 |

## 使用说明

- 请将策略绑定到能够提供 `CurrentValue` 或 `BeginValue` 的投资组合，否则无法计算风险基数。
- 在每次计划交易前调整 `EntryPrice`、`StopLossPercent` 与 `TakeProfitPercent`，以便获得准确的推荐手数。
- `UpdateIntervalSeconds` 必须为正数。数值过小会在日志中产生大量输出，默认的 10 秒与原脚本一致。
- 策略仅用于信息提示，需要配合人工或其他 StockSharp 策略下单。
- Bybit 客服链接以文本形式写入日志，在 Designer 中不会出现可点击的按钮。

## 与 MQL5 版本的差异

- StockSharp 版不绘制图形对象，所有内容都写入日志。
- 使用 `System.Threading.Timer` 驱动定时器，并通过互斥防止任务重叠。
- 当交易日变化或 PnL 更新时自动重置日内盈亏基准，保持统计与账户同步。
- 推荐手数会按 `VolumeStep` 向上取整，确保符合交易商的最小交易单位。
