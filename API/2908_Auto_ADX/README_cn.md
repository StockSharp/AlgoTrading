# Auto ADX 策略

## 概述
**Auto ADX 策略** 是将 MetaTrader 专家顾问 `Auto ADX.mq5` 移植到 StockSharp 高级 API 的版本。策略基于平均趋向指数（ADX）及其 +DI/-DI 分量来判断趋势方向，同时保留原有的止损、止盈、反向平仓以及按点数计算的移动止损机制，并使用 StockSharp 的蜡烛订阅与指标绑定框架。

## 交易逻辑
- **数据来源**：订阅可配置的蜡烛类型（默认：1 小时），只在蜡烛完全结束后处理，避免盘中噪音。
- **ADX 计算**：通过 `BindEx` 绑定单个 `AverageDirectionalIndex` 指标，可同时获取平滑的 ADX 数值以及 +DI、-DI 方向指标。
- **做多条件**：
  - +DI 大于 -DI，表明多头动能占优；
  - 当前 ADX 高于设定阈值；
  - 当前 ADX 高于上一根蜡烛的 ADX，趋势强度增加。
- **做空条件**：
  - -DI 大于 +DI，空头动能占优；
  - 当前 ADX 低于设定阈值；
  - 当前 ADX 低于上一根蜡烛，趋势强度下降。
- **反向模式**：默认开启 `ReverseSignals`，若持仓方向与 DI 或 ADX 斜率条件相反，则立即平仓。
- **仓位管理**：下单数量使用策略 `Volume`，在出现反向信号时先调用 `ClosePosition()` 全部平仓，再评估新的入场机会。

## 风险控制
- **止损 / 止盈**：根据点数参数换算成绝对价格距离，利用 `StartProtection` 自动下达保护单，可选择使用市价执行。
- **移动止损**：复刻原始 EA 的点数移动逻辑：
  - 仅当浮动盈利超过设定的移动距离时才激活；
  - 止损价每次至少推进 `TrailingStepPips` 点；
  - 多单跌破移动止损、空单上破移动止损时立即平仓。
- **点值换算**：仿照 MQL 实现，点值等于 `PriceStep`，若品种为 3 位或 5 位小数报价则乘以 10，保证外汇品种行为一致。

## 参数说明
| 参数 | 默认值 | 描述 |
| --- | --- | --- |
| `StopLossPips` | 50 | 止损距离（点）。为 0 时禁用止损。 |
| `TakeProfitPips` | 50 | 止盈距离（点）。为 0 时禁用止盈。 |
| `TrailingStopPips` | 5 | 移动止损距离（点）。为 0 时禁用移动止损。 |
| `TrailingStepPips` | 5 | 移动止损每次推进所需的最小盈利（点），启用移动止损时必须大于 0。 |
| `AdxPeriod` | 14 | ADX 指标平滑周期。 |
| `AdxLevel` | 30 | ADX 进入过滤阈值。 |
| `ReverseSignals` | true | 信号反向时是否立即平仓。 |
| `CandleType` | 1 小时 | 使用的蜡烛类型。 |

## 实现细节
- 使用 `BindEx` 直接获取 `AverageDirectionalIndexValue`，无需手动从指标缓存读取，符合仓库规范。
- 移动止损记录最近的止损价，仅当价格继续朝盈利方向前进至少 `TrailingStepPips` 点时才移动，保持与原始 EA 相同的阶梯式推进效果。
- C# 源码中的所有注释均为英文，符合仓库要求。
- 仅提供 C# 版本，文件位于 `API/2908_Auto_ADX/CS/AutoAdxStrategy.cs`，按需求未生成 Python 版本。

## 使用建议
1. 确认证券的 `PriceStep` 设置正确，以保证点值换算准确。
2. 根据交易品种的波动性调整 `AdxLevel`，数值越高，信号越少但趋势确认越严格。
3. 若不需要移动止损，将 `TrailingStopPips` 设为 0；此时 `TrailingStepPips` 自动忽略，行为与原 EA 一致。
4. 在历史数据上回测不同市场，验证点数保护距离和 ADX 斜率过滤的有效性。
