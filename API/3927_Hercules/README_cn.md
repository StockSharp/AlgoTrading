# Hercules 策略

Hercules 策略是 MetaTrader 专家顾问 **Hercules v1.3 (Majors)** 的 StockSharp 版本。策略利用快慢均线交叉，并结合多时间框架过滤条件，同时为每个信号管理两个独立的止盈目标。

## 交易逻辑

* **信号准备**：计算基于收盘价的 EMA(1) 与基于开盘价的 SMA(72)，识别最近一根或倒数第二根K线发生的交叉。将两条均线的值求平均得到交叉价格，再向上/向下偏移 `TriggerPips` 形成触发价格。
* **执行窗口**：交叉出现后，信号在两个完整K线周期内有效。只有当当前收盘价在该窗口内突破触发价时才会下单。
* **过滤条件**：
  * H1 RSI（默认周期 `RsiPeriod`，典型价输入）在做多时必须高于 `RsiUpper`，做空时必须低于 `RsiLower`。
  * 当前收盘价需要突破 `LookbackMinutes` 分钟窗口内的最高/最低价。
  * 日线 Envelope（SMA 24，偏移 `DailyEnvelopeDeviation`%）要求收盘价突破带状区间的对应边界。
  * H4 Envelope（SMA 96，偏移 `H4EnvelopeDeviation`%）提供第二层趋势确认。
* **风险控制**：止损位设置在向前数第四根K线的高点或低点。下单量可以固定为 `OrderVolume`，也可以根据 `RiskPercent` 占组合价值的比例自动计算。
* **仓位管理**：每次信号会开出两笔相同手数的市价单。第一笔在 `TakeProfitFirstPips` 处止盈，第二笔在 `TakeProfitSecondPips` 处止盈，`TrailingStopPips` 用于两笔仓位的移动止损。当止损或两个止盈全部触发后，策略会在 `BlackoutHours` 小时内暂停新的交易。

## 参数

| 参数 | 说明 |
| --- | --- |
| `OrderVolume` | 每笔市价单的基础手数。 |
| `UseMoneyManagement` | 是否根据止损距离和 `RiskPercent` 自动调整手数。 |
| `RiskPercent` | 每笔交易允许承担的组合风险百分比。 |
| `TriggerPips` | 触发价与交叉价之间的距离。 |
| `TrailingStopPips` | 移动止损的点数。 |
| `TakeProfitFirstPips` | 第一目标的止盈距离。 |
| `TakeProfitSecondPips` | 第二目标的止盈距离。 |
| `FastPeriod` | 快速 EMA 的周期。 |
| `SlowPeriod` | 慢速 SMA 的周期。 |
| `RsiPeriod` | RSI 滤波器的周期。 |
| `RsiUpper` / `RsiLower` | 多空方向使用的 RSI 阈值。 |
| `LookbackMinutes` | 计算近期高低点的时间窗口（分钟）。 |
| `BlackoutHours` | 交易完成后冻结新信号的小时数。 |
| `DailyEnvelopePeriod` / `DailyEnvelopeDeviation` | 日线 Envelope 的参数。 |
| `H4EnvelopePeriod` / `H4EnvelopeDeviation` | H4 Envelope 的参数。 |
| `CandleType` | 主交易时间框架。 |
| `RsiTimeFrame` | 计算 RSI 使用的时间框架。 |
| `DailyTimeFrame` | 计算日线 Envelope 的时间框架。 |
| `H4TimeFrame` | 计算 H4 Envelope 的时间框架。 |

## 文件结构

* `CS/HerculesStrategy.cs` – Hercules 策略的 C# 实现。
* `README.md` – 英文说明。
* `README_ru.md` – 俄文说明。
* `README_cn.md` – 中文说明。
