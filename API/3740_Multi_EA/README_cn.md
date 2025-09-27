# MultiStrategyEA v1.2（StockSharp 版本）

## 概述
本策略基于 MetaTrader 平台的 **MultiStrategyEA v1.2**，使用 StockSharp 的高级 API 重新实现。原始 EA 通过七个指标模块、网格与加倍手数管理订单。移植版本仅保留信号生成部分，并使用单一净头寸进行交易，以便更好地集成到 StockSharp Designer、Runner 等工具中。网格、递增手数、自动资金管理等复杂模块均未移植。

## 指标模块
策略在所选周期上计算以下指标：

1. **加速/减速指标（AC）**：计算 Awesome Oscillator 与其 5 周期均线的差值。当当前值高于 `AcLevel` 且继续上升/下降时给出买入/卖出信号。
2. **平均趋向指数（ADX）**：当 ADX 超过 `AdxTrendLevel` 且主导方向的 DI 超过 `AdxDirectionalLevel` 时确认趋势方向。
3. **Awesome Oscillator（AO）**：当指标突破 `AoLevel` 并维持同方向变化时触发信号。
4. **DeMarker**：当指标从超卖（`100 - DeMarkerThreshold`）或超买（`DeMarkerThreshold`）区域离开时提示反转。
5. **Force Index + 布林带**：价格触及布林带同时 Force Index（按照原始 EA 相同方式缩放）超过 `ForceConfirmationLevel` 才认为有效。`BandDistanceFilter` 可限制布林带宽度（以点数表示）。
6. **资金流量指数（MFI）**：与 DeMarker 类似，识别超买/超卖反转。
7. **MACD + 随机指标**：MACD 必须达到 `MacdLevel` 且高于/低于信号线，同时随机指标超过/低于 `StochasticLevel` 并与信号线方向一致。

每个模块都会根据最新完成的 K 线投票：买入、卖出或保持中性。

## 共识逻辑
- `TradeAllStrategies = true`（默认）时，至少需要 `RequiredConfirmations` 个买入或卖出投票，并且没有相反投票，才会进场。
- `TradeAllStrategies = false` 时，单个投票即可触发交易。
- 当启用 `CloseInReverse` 时，若出现反向共识，策略会先平掉当前仓位再开新仓。

移植版本只维护一个净头寸，不再区分各模块独立持仓。

## 风险管理
- `StopLossPips` 与 `TakeProfitPips` 根据品种的 `PriceStep` 自动换算为价格偏移。当价格步长为 0.001 或 0.00001 时，会自动乘以 10 以匹配外汇“pip”。
- 每根完成的 K 线都会检查最高价/最低价是否触及止损或止盈，触发后立即平仓。

## 与 MT5 版本的差异
- 无网格、无马丁加仓，仓位大小仅由 `Volume` 控制。
- 未实现 MT5 中的 `CloseOrdersType` 关闭模式，退出主要依赖止损/止盈或 `CloseInReverse` 反向信号。
- 指标模块仅保留最常用的判断方式，未涵盖原始 EA 中所有枚举组合。
- 自动批量控制、账户保护及其他账户层面逻辑未包含在内。

## 参数
| 参数 | 说明 |
|------|------|
| `CandleType` | 所有指标使用的 K 线类型。 |
| `Volume` | 共识信号出现时的交易数量。 |
| `TradeAllStrategies` | 是否需要多个模块共识。 |
| `RequiredConfirmations` | 开仓所需的一致投票数量（在共识模式下）。 |
| `CloseInReverse` | 出现反向信号时先平仓再反向开仓。 |
| `StopLossPips` / `TakeProfitPips` | 以点数表示的止损和止盈距离。 |
| 其他 `Use*` 与阈值/周期参数 | 对应模块的启用开关及阈值设置。 |

## 使用建议
1. 确保历史数据足以让所有指标形成；初期可能需要数十根 K 线。
2. 根据品种波动情况调整各模块阈值；默认值与 MT5 输入参数一致。
3. 如果禁用部分模块，请同步调低 `RequiredConfirmations`，避免策略因为投票不足而不交易。
4. 策略仅维护单笔净头寸，适合集成到 StockSharp Designer、Runner 等高层工具中，无需额外的投资组合路由。

## 注意事项
由于省略了网格、资金管理和部分退出逻辑，运行结果与 MT5 原版会有差异。本移植旨在提供清晰、可扩展的信号框架，用户可以在此基础上继续定制更复杂的仓位管理或组合策略。
