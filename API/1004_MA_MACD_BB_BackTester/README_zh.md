# MA MACD BB BackTester

该策略可在三种指标之间选择：简单移动平均线交叉、MACD 交叉或布林带突破。任意时刻仅启用一种模式，并可选择做多或做空方向。

## 参数
- `CandleType` — K线周期。
- `Indicator` — 使用的指标（MA、MACD、BB）。
- `Direction` — 交易方向（Long 或 Short）。
- `MaLength` — 移动平均线周期。
- `FastLength` — MACD 快速 EMA 周期。
- `SlowLength` — MACD 慢速 EMA 周期。
- `SignalLength` — MACD 信号线周期。
- `BbLength` — 布林带周期。
- `BbMultiplier` — 布林带系数。
- `StartDate` — 开始日期。
- `EndDate` — 结束日期。
