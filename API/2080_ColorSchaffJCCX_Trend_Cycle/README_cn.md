# 颜色 Schaff JCCX 趋势循环策略

该策略是 MQL5 专家 `Exp_ColorSchaffJCCXTrendCycle` 的 C# 版本，
使用基于 JCCX 算法的 **Schaff 趋势循环 (STC)** 振荡器。

## 交易逻辑

* 在每根完成的蜡烛上计算 Schaff 趋势循环。
* 当振荡器在高于 `High Level` 后跌破该水平时，开多单并平空单。
* 当振荡器在低于 `Low Level` 后突破该水平时，开空单并平多单。

## 参数

| 名称 | 说明 |
|------|------|
| Fast JCCX | 指标中使用的快速 JCCX 周期。 |
| Slow JCCX | 指标中使用的慢速 JCCX 周期。 |
| Smoothing | JCCX 的 JJMA 平滑因子。 |
| Phase | JJMA 相位值。 |
| Cycle | Schaff 趋势计算的周期长度。 |
| High Level | 振荡器的上触发水平。 |
| Low Level | 振荡器的下触发水平。 |
| Open Long | 允许开多。 |
| Open Short | 允许开空。 |
| Close Long | 允许平多。 |
| Close Short | 允许平空。 |

## 说明

策略使用 StockSharp 的高级 API，并订阅蜡烛数据。只对**已完成**的蜡烛作出反应。资金管理和风险控制仅用于演示目的。
