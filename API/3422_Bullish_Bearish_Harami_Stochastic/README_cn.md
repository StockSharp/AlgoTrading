# Bullish & Bearish Harami Stochastic 策略

**Bullish & Bearish Harami Stochastic Strategy** 是将 MetaTrader 专家顾问 `expert_abh_bh_stoch.mq5`（位于 `MQL/310`）迁移到 StockSharp 的结果。原版 EA 利用「孕线」蜡烛形态配合随机指标 Stochastic 的确认信号。本次 C# 实现依托 StockSharp 高级 API 复刻全部逻辑，并提供详细日志与图表输出，方便跟踪策略行为。

## 核心思想

- 根据最近两根已完成 K 线检测 Bullish Harami（看涨孕线）和 Bearish Harami（看跌孕线）形态。
- 只有当随机指标 %D 线跌破超卖阈值时才确认做多信号，当 %D 突破超买阈值时才确认做空信号。
- 当 %D 线向上穿越任一退出阈值时平掉空单，当 %D 线向下跌破阈值时了结多单。

## 参数说明

| 参数 | 含义 | 默认值 |
|------|------|--------|
| `CandleType` | 用于识别形态的 K 线周期。 | `1 小时` |
| `StochasticKPeriod` | 随机指标 %K 的回看周期。 | `47` |
| `StochasticDPeriod` | %D 线的平滑周期。 | `9` |
| `StochasticSlowing` | %K 额外平滑系数（MT5 的 Slowing）。 | `13` |
| `MovingAveragePeriod` | 计算蜡烛实体平均值的样本数量。 | `5` |
| `OversoldLevel` | 确认多头信号的超卖阈值。 | `30` |
| `OverboughtLevel` | 确认空头信号的超买阈值。 | `70` |
| `ExitLowerLevel` | 触发离场的随机指标下限。 | `20` |
| `ExitUpperLevel` | 触发离场的随机指标上限。 | `80` |

## 交易规则

### 多头入场
1. 最近两根 K 线形成看涨孕线（长阴线后跟随一根被其包裹的小阳线，且处于下行环境）。
2. 当前随机指标 %D 值不高于 `OversoldLevel`。
3. 当前没有持仓或为净空头（`Position <= 0`）。
4. 按市场价买入设定的 `Volume`，必要时先平掉空单实现反手。

### 空头入场
1. 检测到看跌孕线（长阳线后跟随一根被其包裹的小阴线，且处于上行趋势）。
2. 随机指标 %D 不低于 `OverboughtLevel`。
3. 当前没有空单或为净多头（`Position >= 0`）。
4. 按市场价卖出，若已有多单则先行平仓。

### 离场策略
- **平空：** 当 %D 向上突破 `ExitLowerLevel` 或 `ExitUpperLevel` 时，全部平掉空单。
- **平多：** 当 %D 向下跌破 `ExitUpperLevel` 或 `ExitLowerLevel` 时，立即平掉多单。

## 文件结构

- `CS/BullishBearishHaramiStochasticStrategy.cs` — 策略的 StockSharp C# 实现。
- `README.md` — 英文文档。
- `README_ru.md` — 俄文文档。
- `README_cn.md` — 本文件，中文说明。

> **提示：** 根据任务要求，暂未提供 Python 版本。
