# XIT 三均线交叉策略
[English](README.md) | [Русский](README_ru.md)

本策略是在 StockSharp 平台上重构的 MetaTrader 5 专家顾问 **XIT_THREE_MA_CROSS.mq5**。它在主图上排列三条移动平均线，利用 MACD 动能差值进行确认，并以 ATR 风险距离计算仓位规模。策略定位于趋势跟随并结合动能确认，适合在所选周期内保持延续性的外汇或指数市场。

## 概览

- **市场类型**：适用于在选定周期内形成连续趋势的品种。
- **核心指标**：
  - 慢速、过渡和快速移动平均线（类型可选），全部基于交易时间框。
  - 基于 EMA 的 MACD，用于判断动能方向以及 MACD 与信号线之间的距离。
  - 两个相同长度、独立时间框的 ATR，分别生成止损和止盈距离。
- **交易方向**：支持做多与做空。
- **仓位控制**：按照账户风险百分比与 ATR 止损距离计算下单数量；若缺少必要的合约参数，则回退至默认 `Volume` 数值。

## 交易逻辑

### 做多条件

当以下条件在一根已完成的 K 线上同时满足时开多仓：

1. MACD 主线较上一根 K 线抬升（`MACD[t] > MACD[t-1]`）。
2. MACD 信号线较上一根 K 线上升。
3. 主线高于信号线，差值不少于 `MacdTriggerPoints * PriceStep`。
4. 过渡均线较上一值上升。
5. 快速均线上升。
6. 过渡均线位于慢速均线上方。
7. 快速均线位于过渡均线上方。
8. 两条 ATR 均产生了有效值，用于设置保护位。

### 做空条件

做空规则与做多对称：

1. MACD 主线较上一根 K 线下降。
2. MACD 信号线下降。
3. 信号线高于主线且差值大于等于 `MacdTriggerPoints * PriceStep`。
4. 过渡均线下降。
5. 快速均线下降。
6. 过渡均线低于慢速均线。
7. 快速均线低于过渡均线。
8. 两个 ATR 序列均已完成计算。

### 离场机制

- **多头**：当快速均线跌破过渡均线，或价格触及 ATR 止损/止盈水平时全部平仓。
- **空头**：当快速均线上穿过渡均线，或价格触及 ATR 水平时止盈或止损。
- 策略在离场后等待下一根 K 线再评估新信号，以保持与原版 EA 相同的节奏。

## 风险管理

- **止损**：距离等于 `AtrStopCandleType` 时间框上 ATR 的最新值。多头止损价 = `Entry - ATR`，空头止损价 = `Entry + ATR`。
- **止盈**：距离使用 `AtrTakeCandleType` 的 ATR 值，方向与止损相反。
- **风险百分比**：根据止损距离估算每手亏损金额。如果合约提供 `PriceStep` 与 `PriceStepCost`，则按最小跳动价值计算；否则采用绝对价格距离。仓位 = 账户当前价值的 `RiskPercent%` / 单位风险，并向下取整到最近的 `VolumeStep`。

## 参数

| 参数 | 说明 | 默认值 |
| --- | --- | --- |
| `CandleType` | 计算均线与 MACD 的主时间框。 | 1 小时 K 线 |
| `SlowMaLength` / `IntermediateMaLength` / `FastMaLength` | 三条均线的周期。 | 60 / 14 / 4 |
| `SlowMaType`, `IntermediateMaType`, `FastMaType` | 均线类型（Simple、Exponential、Smoothed、Weighted）。 | Simple |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | MACD 的快速、慢速与信号 EMA 周期。 | 12 / 26 / 9 |
| `MacdTriggerPoints` | MACD 与信号线之间最小距离，按品种最小变动单位计，运行时通过 `PriceStep` 转换。 | 7 |
| `AtrLength` | 两个 ATR 的周期。 | 14 |
| `AtrTakeCandleType` / `AtrStopCandleType` | ATR 止盈和止损的时间框。 | 4 小时 K 线 |
| `RiskPercent` | 单笔交易允许承担的账户风险百分比。 | 10% |

## 使用建议

1. 使用包含准确 `PriceStep`、`PriceStepCost` 与 `VolumeStep` 的标的，以获得正确的仓位规模。
2. 确保所有订阅时间框（`CandleType`、`AtrTakeCandleType`、`AtrStopCandleType`）均有足够历史数据，否则策略会等待 ATR 成型。
3. 算法仅在 K 线收盘后评估信号，与原始 EA 读取当前/上一指标缓冲区的方式一致。
4. 可根据交易品种的波动特征调整均线类型，以获得更平滑或更敏捷的过滤效果。

## 文件

- `CS/XitThreeMaCrossStrategy.cs` —— 使用 StockSharp 高级 API 编写的 C# 实现，包含 ATR 订阅与风险控制。
- `README.md` —— 英文说明。
- `README_ru.md` —— 俄文说明。
