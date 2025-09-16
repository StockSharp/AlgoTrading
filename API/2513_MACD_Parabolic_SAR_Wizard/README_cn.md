# MACD 抛物线SAR向导策略

## 概述
该策略是对 MQL5 向导自动生成的 “MACD + Parabolic SAR” 专家顾问的 StockSharp 实现，结合了 MACD 动量与抛物线 SAR 趋势方向。通过为每个指标赋予 0..100 的归一化评分并按权重合成，复现了原始向导模板的评分决策流程。

## 交易逻辑
- **指标**
  - *MACD (12, 24, 9)*：当 MACD 直方图大于 0 时认定为多头动量，小于 0 时认定为空头动量。
  - *抛物线 SAR (0.02, 0.2)*：收盘价高于 SAR 点表示上升趋势，低于 SAR 点表示下降趋势。
- **评分构建**
  - MACD 在多头方向上给出 100（看涨）或 0（看跌）的评分，空头方向使用相反的取值。
  - 抛物线 SAR 采用相同逻辑，在趋势与方向一致时给出 100 分。
  - 最终的多空评分通过 `MacdWeight` 与 `SarWeight` 线性组合。默认权重 0.9 / 0.1 让 MACD 像原模板一样占据主导。
- **入场条件**
  - 多头评分：`bullScore = macdBull * MacdWeight + sarBull * SarWeight`。
  - 空头评分：`bearScore = macdBear * MacdWeight + sarBear * SarWeight`。
  - 当 `bullScore >= OpenThreshold`（默认 20）时买入或从空头反手为多头。
  - 当 `bearScore >= OpenThreshold` 时卖出或从多头反手为空头。
- **出场条件**
  - 多头持仓在 `bearScore >= CloseThreshold`（默认 100）时全部平仓。
  - 空头持仓在 `bullScore >= CloseThreshold` 时全部平仓。
  - 先评估出场信号，再评估入场信号，以模拟向导专家优先解除冲突仓位的行为。

## 风险控制
- `StopLossPoints` 与 `TakeProfitPoints` 采用与向导相同的点值设置。参数会根据标的的 `PriceStep` 转换为实际价格距离并传入 `StartProtection`。
- 任意一个参数为 0 时，相应的止损/止盈保护会被禁用。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `MacdFastPeriod` | MACD 快速 EMA 周期 | 12 |
| `MacdSlowPeriod` | MACD 慢速 EMA 周期 | 24 |
| `MacdSignalPeriod` | MACD 信号 SMA 周期 | 9 |
| `MacdWeight` | MACD 评分权重（0..1） | 0.9 |
| `SarWeight` | 抛物线 SAR 评分权重（0..1） | 0.1 |
| `OpenThreshold` | 开仓/反手所需的最低评分 | 20 |
| `CloseThreshold` | 平仓所需的反向评分阈值 | 100 |
| `SarStep` | 抛物线 SAR 加速步长 | 0.02 |
| `SarMax` | 抛物线 SAR 最大加速值 | 0.2 |
| `StopLossPoints` | 止损距离（点） | 50 |
| `TakeProfitPoints` | 止盈距离（点） | 115 |
| `CandleType` | 用于计算的 K 线类型 | 15 分钟 |

## 使用提示
- 默认参数完全对应 `.mq5` 模板，可保证与原始专家顾问一致的行为。
- 调整 `MacdWeight`、`SarWeight` 与阈值即可控制入场/出场的敏感度。例如提升 `OpenThreshold` 可以减少噪声交易。
- `_lastBullScore` 和 `_lastBearScore` 字段在每根 K 线都会更新，可用于记录或扩展可视化，以跟踪组合评分的变化。
- 策略仅处理完成的 K 线，确保行情源能提供完整的收盘更新。
- 止损止盈以点值表示，务必确认标的的 `PriceStep` 与预期一致，以免保护距离出现偏差。
