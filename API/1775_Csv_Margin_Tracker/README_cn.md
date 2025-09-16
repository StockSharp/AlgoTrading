# CSV Margin Tracker 策略
[English](README.md) | [Русский](README_ru.md)

该工具型策略按固定时间间隔记录投资组合的余额、净值和保证金到 CSV 文件。
当保证金与净值比率超过可配置阈值时，可选择记录提示信息。

## 细节
- **用途**：监控账户风险，在每个间隔内保存最小余额、最小净值和最大保证金。
- **数据输出**：写入 `margintracker_<portfolio>.csv`。
- **提示**：两个保证金水平在冷却时间后触发日志提示。

## 参数
- `IntervalSeconds` – 聚合时间间隔。
- `MailAlert` – 是否启用保证金提示。
- `MailAlertIntervalSeconds` – 提示之间的最小延迟。
- `MailAlertMarginLevel1` – 第一个保证金/净值阈值。
- `MailAlertMarginLevel2` – 第二个保证金/净值阈值。
