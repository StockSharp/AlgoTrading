# Gann Grid 策略

该策略将 `MQL/25065/Gann Grid.mq4` 中的 **Gann Grid** 专家顾问迁移到 StockSharp 的高级 API。原始脚本依赖人工绘制的图形对象并在多个时间框架上过滤；C# 版本保留总体流程，同时用指标计算替代手工操作，使策略可以完全自动化运行。

## 交易逻辑

1. **合成 Gann 网格**：在主时间框架上取 `AnchorPeriod` 根 K 线的最高价和最低价，近似于 MetaTrader 中手绘的两条 Gann 线。向上突破最高价触发做多，向下跌破最低价触发做空。
2. **趋势确认**：在 `TrendCandleType` 指定的高一级时间框架上计算快、慢两条线性加权均线（LWMA），方向必须支持当前突破。
3. **动量过滤**：同一高时间框架上的 Momentum 指标与当前收盘价的百分比差值需大于 `MomentumThreshold`，以保证行情具有足够动能。
4. **MACD 过滤**：通过 `MacdCandleType` 订阅的蜡烛序列计算 MACD（默认 12/26/9）。MACD 线必须与信号线、零轴在同一侧。
5. **风险控制**：从入场价开始应用对称的止盈与止损距离，可选的保本和跟踪止损模块复现原始 MQL 代码中的资金保护逻辑。

策略仅处理收盘后的 K 线，与原脚本检测新柱的方式保持一致。

## 与 MQL 版本的差异

- MetaTrader 版本需要手动绘制 `GANNGRID` 对象。移植版本使用 Highest/Lowest 指标自动生成网格，更适合回测与批量运行。
- MetaTrader 中的 Momentum 指标围绕 100 波动；StockSharp 的 `Momentum` 返回价格差值，因此策略将其换算成相对于收盘价的百分比再与 `MomentumThreshold` 比较。
- 邮件、推送等通知以及所有图形相关操作均被移除。
- 头寸管理通过市价单执行，不再修改已有挂单，因为 StockSharp 更侧重仓位级别的控制。

## 参数

| 名称 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | 5 分钟 | 用于识别突破的主时间框架。 |
| `TrendCandleType` | `DataType` | 15 分钟 | 计算 LWMA 与 Momentum 的高一级时间框架。 |
| `MacdCandleType` | `DataType` | 1 天 | 计算 MACD 的时间框架。 |
| `FastMaPeriod` | `int` | 6 | 高时间框架上的快 LWMA 长度。 |
| `SlowMaPeriod` | `int` | 85 | 高时间框架上的慢 LWMA 长度。 |
| `MomentumPeriod` | `int` | 14 | Momentum 指标周期。 |
| `MomentumThreshold` | `decimal` | 0.3 | Momentum 相对价格的最小百分比偏离。 |
| `AnchorPeriod` | `int` | 100 | 参与生成合成网格的主时间框架 K 线数量。 |
| `TakeProfitOffset` | `decimal` | 0.005 | 与入场价的绝对止盈距离。 |
| `StopLossOffset` | `decimal` | 0.002 | 与入场价的绝对止损距离。 |
| `EnableTrailing` | `bool` | `true` | 是否启用跟踪止损。 |
| `TrailingActivation` | `decimal` | 0.003 | 跟踪止损启动前所需的利润。 |
| `TrailingStep` | `decimal` | 0.0015 | 启动后跟踪止损与局部高点/低点的距离。 |
| `EnableBreakEven` | `bool` | `true` | 是否启用自动保本。 |
| `BreakEvenTrigger` | `decimal` | 0.0025 | 启动保本所需的利润。 |
| `BreakEvenOffset` | `decimal` | 0.0 | 保本平仓时相对入场价的偏移量。 |
| `MacdFastPeriod` | `int` | 12 | MACD 中快 EMA 的周期。 |
| `MacdSlowPeriod` | `int` | 26 | MACD 中慢 EMA 的周期。 |
| `MacdSignalPeriod` | `int` | 9 | MACD 信号 EMA 的周期。 |

所有距离参数均以价格绝对值表示，请结合交易品种的最小报价单位调整（例如 0.001 ≈ 外汇五位报价的 10 点）。

## 使用建议

1. 将策略绑定到目标证券，并设置需要的蜡烛类型。如果希望只使用单一时间框架，可以让多个参数指向同一 `DataType`。
2. 根据市场波动调整 `AnchorPeriod` 及各类价格偏移。
3. 按照风险偏好启用或关闭保本、跟踪止损功能。
4. 启动策略后，系统会自动订阅所需蜡烛并通过市价单管理仓位。

## 文件结构

- `CS/GannGridStrategy.cs`：策略实现。
- `README.md`：英文说明。
- `README_ru.md`：俄文说明。
- `README_cn.md`：中文说明。
