# FmOne Scalping Strategy

## 概述
FmOne Scalping Strategy 是 FMOneEA MetaTrader 4 专家顾问的简化移植。该策略结合快慢指数移动平均线与 MACD 指标，在任何时间框架上捕捉短期动量。

## 工作原理
1. 快速 EMA 与慢速 EMA 定义当前趋势。
2. MACD 柱状图确认趋势方向的动量。
3. 当快速 EMA 位于慢速 EMA 之上且 MACD 柱状图为正时开多单。
4. 当快速 EMA 位于慢速 EMA 之下且 MACD 柱状图为负时开空单。
5. 每个仓位都通过可配置的止损和止盈保护，可选的追踪止损可以跟随盈利移动。

## 参数
- **FastMaPeriod** – 快速 EMA 周期。
- **SlowMaPeriod** – 慢速 EMA 周期。
- **MacdSignalPeriod** – MACD 信号线周期。
- **StopLossPercent** – 以入场价百分比表示的止损。
- **TakeProfitPercent** – 以入场价百分比表示的止盈。
- **EnableTrailingStop** – 是否启用追踪止损。
- **CandleType** – 使用的K线时间框架。

## 注意
此移植仅实现原始 EA 的核心逻辑。原版中的赎回循环与保本功能被省略，以保持示例简洁。
