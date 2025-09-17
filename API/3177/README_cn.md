# iMA iStochastic Custom 策略

## 概览
本策略在 StockSharp 环境中复刻 MetaTrader 专家顾问 **“iMA iStochastic Custom”**。它将移动平均包络与随机振荡器相结合，只在所选时间框架（`CandleType`）的完整 K 线收盘后评估信号。下文使用与原始 EA 相同的术语。

核心模块：

1. **移动平均包络** – 在基准移动平均值上分别加上 `LevelUpPips` 与 `LevelDownPips`（以点为单位）形成阻力和支撑通道，可选择简单、指数、平滑（SMMA）或线性加权（LWMA）四种算法。
2. **随机振荡器** – %K、%D 以及平滑参数完全对应原版输入，通过 `StochasticLevel1` 与 `StochasticLevel2` 判定超买/超卖区域。
3. **资金管理** – `ManagementMode` 继承了 MQL 的“定量/风险百分比”开关。`FixedLot` 模式下下单量等于 `VolumeValue`；`RiskPercent` 模式则按 `VolumeValue` 指定的权益百分比计算仓位，等价于 `CMoneyFixedMargin` 的行为。
4. **保护机制** – 止损、止盈及追踪距离均以点填写，并在每根收盘 K 线时检查。追踪止损需要价格先运行 `TrailingStopPips`，随后每次至少改善 `TrailingStepPips` 才会上调，逻辑与原 EA 一致。

## 交易规则
多空规则对称：

- **做多**：K 线收盘价高于上轨（`ma + LevelUpPips`），且随机指标任一分量高于 `StochasticLevel1`。
- **做空**：K 线收盘价低于下轨（`ma + LevelDownPips`），且随机指标任一分量低于 `StochasticLevel2`。
- 将 `ReverseSignals` 设为 `true` 可反转方向。

策略始终保持单一净头寸。信号翻转时，会自动下达足够的反向委托以平仓并建立新的方向。

## 风险控制与退出
- **止损 / 止盈**：按 `PriceStep` 将点值换算成价格距离，并以当根 K 线的最高/最低价触发。
- **追踪止损**：价格获利达到 `TrailingStopPips` 后启动，每次至少再改善 `TrailingStepPips` 才会上移/下移止损。
- **资金管理**：风险模式下的手数为 `权益 * VolumeValue / 100 / perUnitRisk`，其中 `perUnitRisk` 表示止损前每单位手数的货币损失。

## 参数一览
| 参数 | 说明 |
|------|------|
| `CandleType` | 使用的时间框架。 |
| `StopLossPips`、`TakeProfitPips` | 止损与止盈距离（点）。 |
| `TrailingStopPips`、`TrailingStepPips` | 追踪止损启动距离与步长（点），为 0 时关闭。 |
| `ManagementMode`、`VolumeValue` | 定量或风险百分比仓位控制。 |
| `MaPeriod`、`MaShift`、`MaMethod` | 移动平均周期、回溯偏移与计算方式。 |
| `LevelUpPips`、`LevelDownPips` | 上下轨偏移（点），下轨可填负值。 |
| `StochasticKPeriod`、`StochasticDPeriod`、`StochasticSlowing` | 随机指标设置。 |
| `StochasticLevel1`、`StochasticLevel2` | 进场确认阈值。 |
| `ReverseSignals` | 是否反向开仓。 |

## 实现细节
- 通过高阶 API (`SubscribeCandles().BindEx(...)`) 订阅蜡烛与指标，避免手动管理数据缓冲。
- 点值会根据品种小数位数自动转换：3/5 位报价将 `PriceStep` 乘以 10。
- 追踪止损在收盘 K 线上执行，若需要更细粒度的移动，可改为订阅逐笔或价格流。
- 按要求未提供 Python 版本，因此没有 `PY` 目录。

## 与原始 EA 的差异
- 风险计算显式依赖 StockSharp 的账户权益与步长报价，等价替代 `CMoneyFixedMargin`。若未配置止损，则风险距离为 0，系统拒绝开仓，与 MQL 的返回零手数完全一致。
- 止损、止盈与追踪判断基于完整 K 线，契合 StockSharp 事件驱动的运行方式，便于回测复现。
- 原版的 `InpPrintLog` 调试输出未移植，改用框架内建的日志机制。

该策略适用于在 StockSharp Designer、Runner 等环境中复用原有的 MetaTrader 交易逻辑，可根据标的和时间框架调整参数。
