# Keltner Seasonal Filter 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

交易带有历史月度收益过滤的肯特纳通道突破。已完成5分钟 K 线上使用 `E = EMA(20)`、`A = ATR(14)`、`M = AtrMultiplier = 2`、`U = E + M × A`、`L = E − M × A`，以及 `SeasonalThreshold = 0.5` 个百分点。必须等待指标形成并允许交易。

`S > SeasonalThreshold`、`Close > U`、`Position <= 0` 时买入；`S < -SeasonalThreshold`、`Close < L`、`Position >= 0` 时卖出。订单数量为 `Volume + abs(Position)`，可完整反转相反持仓。多头止损设为信号 `Close − M × A`，空头为 `Close + M × A`。

每根已完成 K 线先检查止损：多头 `Close <= stop` 或空头 `Close >= stop` 则平仓，清除止损并结束该根处理。否则检查入场，之后多头低于 E 或空头高于 E 时平仓。这是基于信号收盘价、在 K 线收盘检查的手动止损；没有盘中交易所止损、止盈或追踪止损。

必须设置 `SeasonalityHistoryFile`：包含月开盘价和收盘价的 UTF-8 CSV，表头为 `SecurityId,Month,Open,Close`。`Month` 格式为 `yyyy-MM`，数字使用小数点，价格必须为正，同一交易标的的月份不能重复。其他证券 ID 的行被忽略；输入缺失、格式错误或无匹配记录会阻止启动。

每根信号 K 线的 `S` 为过去 `SeasonalityLookbackYears = 10` 年中相同日历月份的 `100 × (Close / Open − 1)` 平均值。至少需要 `MinimumSeasonalSamples = 2` 个观测值。即使文件包含当前月及未来月，也不使用它们。历史不足时不开新仓。应提供该标的的真实月度价格，不提供固定的月份评分表。
