# Fundamental Trader DailyFX 策略

## 概述
本策略是 **Fundamental Trader DailyFX v0.04** MetaTrader EA 的 StockSharp 版本。它读取 DailyFX CSV 日历中的宏观经济数据，在实际值与预期值偏离足够大时自动开仓。迁移过程中保留了原有的事件驱动流程、18 个仓位级别，同时增加了在多品种环境下所需的安全检查。

## 交易逻辑
- 按照定时器周期读取配置的 DailyFX CSV 文件。
- 只处理重要度为 *High* 且时间落在 `WaitingMinutes` 窗口内的事件。
- 如果同时存在 Actual 与 Forecast 字段，则计算相对偏差 `|Actual - Forecast| / |Forecast|`；若 Forecast 缺失，则改为与 Previous 对比。
- 偏差大小决定使用的仓位级别。利好消息在货币位于合约基准货币时买入，在货币位于计价货币时卖出；利空消息方向相反。
- 根据 `RiskPips` 和 `RewardMultiplier` 利用最新的买卖价生成止损与止盈价格。
- 通过 Level1 行情持续监控止损/止盈。如果启用了 `EnableCloseByTime`，还会在 `CloseAfterMinutes` 后强制平仓。

## 参数
- **Calendar File** (`CalendarFilePath`): DailyFX CSV 日历的路径，需要用户自行下载或生成。
- **Waiting Minutes** (`WaitingMinutes`): 事件前后允许交易的时间窗口（分钟）。
- **Enable Timed Exit** (`EnableCloseByTime`): 是否启用定时平仓。
- **Timed Exit Minutes** (`CloseAfterMinutes`): 定时平仓触发的持仓时间。
- **Risk (pips)** (`RiskPips`): 止损距离（点）。
- **Reward Multiplier** (`RewardMultiplier`): 止盈距离相对于止损的倍数。
- **Timer Frequency (sec)** (`TimerFrequencySeconds`): 重新读取 CSV 与检查定时器的频率。
- **Calendar TZ Offset (h)** (`CalendarTimeZoneOffsetHours`): CSV 时间需要调整的时区偏移（小时）。
- **Currency Map** (`CurrencyMap`): 日历货币与可交易品种的映射，例如 `EUR=EURUSD;USD=EURUSD;JPY=USDJPY`。
- **VolumeLevel1 .. VolumeLevel18**: 各个偏差区间使用的仓位规模，默认值与 EA 一致，可参与优化。

## 实现细节
- CSV 解析由内置轻量级函数完成，支持带引号的字段，无需额外依赖库。
- 通过对每个品种订阅 Level1 行情来替代 MQL 的 `MarketInfo` 调用，并据此管理止损/止盈。
- 下单前会按照合约的 `VolumeMin`/`VolumeStep` 调整数量，避免“无效手数”问题。
- 止损和止盈均在策略内部通过实时行情触发，不依赖交易服务器的组合委托。
- 原 EA 使用的 `str2double.dll` 在 C# 中改为对字符串进行清洗，支持货币符号、千分位分隔符及括号表示的负数。

## 使用步骤
1. 下载 DailyFX 日历 CSV，并定期更新（可借助外部脚本或计划任务）。
2. 配置 `CurrencyMap`，确保每个日历货币映射到连接中可用的交易品种。
3. 根据策略计划设置风险、收益以及仓位分级参数。
4. 启动策略，系统会订阅 Level1 行情，按设定频率检查 CSV，并在发布数据时自动开平仓。

## 与原始 EA 的差异
- 不再内置 HTTP 下载功能，CSV 文件需由用户或外部流程提供。
- 使用 Level1 行情模拟 `MarketInfo` 并实现止损/止盈逻辑。
- 下单数量自动符合交易所限制，避免原版本修复过的“手数无效”错误。
- 定时平仓通过策略计时器实现，而不是依赖 MQL 的主循环。
- 提供更详细的日志信息，便于在多品种环境中排查映射或行情缺失的问题。
