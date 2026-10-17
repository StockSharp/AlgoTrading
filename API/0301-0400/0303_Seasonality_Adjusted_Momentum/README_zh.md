# Seasonality Adjusted Momentum 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

将动量与历史月度收益结合。使用已完成的5分钟 K 线计算 `M = Momentum(14)` 及其 `A = SMA(14)`。空仓时，`S >= SeasonalityThreshold` 且 `M > A` 则买入；`S <= -SeasonalityThreshold` 且 `M < A` 则卖出。默认 `SeasonalityThreshold = 0.5`，单位为百分点。

多头季节条件失效或 `M < A` 时平仓；空头条件失效或 `M > A` 时平仓。每笔信号订单后暂停 `CooldownBars = 120` 根已完成 K 线的后续信号判断。另有按实际成交价计算的本地原生 `StopLossPercent = 2` 保护，通过 bid/ask 监控并市价退出，不受信号冷却限制。

必须设置 `SeasonalityHistoryFile`：包含月开盘价和收盘价的 UTF-8 CSV，表头为 `SecurityId,Month,Open,Close`。`Month` 格式为 `yyyy-MM`，数字使用小数点，价格必须为正，同一交易标的的月份不能重复。其他证券 ID 的行被忽略；输入缺失、格式错误或无匹配记录会阻止启动。

每根信号 K 线的 `S` 为过去 `SeasonalityLookbackYears = 10` 年中相同日历月份的 `100 × (Close / Open − 1)` 平均值。至少需要 `MinimumSeasonalSamples = 2` 个观测值。即使文件包含当前月及未来月，也不使用它们。历史不足时不开新仓。应提供该标的的真实月度价格，不提供固定的月份评分表。
