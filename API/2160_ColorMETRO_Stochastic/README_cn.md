# ColorMETRO 随机指标策略

该策略是 MQL5 专家 **exp_colormetro_stochastic.mq5** 的 C# 版本。原始脚本使用自定义 ColorMETRO Stochastic 指标，这里改用 StockSharp 内置的 `StochasticOscillator` 并依据交叉信号进行交易。

## 逻辑
- 默认订阅 8 小时K线（可配置）。
- 计算随机指标，参数包括：
  - %K 周期 (`KPeriod`)
  - %D 周期 (`DPeriod`)
  - 额外平滑 (`Slowing`)
- 记录上一根 K、D 值以检测交叉。
- 当 %K 上穿 %D 时 **买入**。
- 当 %K 下穿 %D 时 **卖出**。
- 通过 `StartProtection` 设定 2% 的止损和止盈。

## 参数
| 名称 | 说明 |
|------|------|
| `KPeriod` | %K 计算周期，默认 5。 |
| `DPeriod` | %D 平滑周期，默认 3。 |
| `Slowing` | 额外平滑值，默认 3。 |
| `CandleType` | 使用的K线时间周期，默认 8 小时。 |

## 说明
原版 MQL 使用带有快速与慢速步进线的 ColorMETRO 指标。本版本使用标准随机指标来近似其信号。
