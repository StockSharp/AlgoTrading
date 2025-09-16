# CCIT3 零轴反转策略

## 概述
CCIT3 Zero Cross 策略是 MetaTrader 5 专家顾问的 StockSharp 版本，核心思想是交易 CCIT3 振荡指标穿越零轴的时刻。该指标通过将 Tillson T3 平滑链应用到商品通道指数（CCI）上构建。当平滑后的数值改变正负号时，策略会按照信号方向开仓，或者在启用了反向模式时先平掉当前头寸并建立反向仓位。

## 交易逻辑
- 按照所选价格类型与周期计算 CCI。
- 使用 Tillson T3 滤波器对振荡器进行平滑，提供两个计算模式：
  - **Simple**：六阶段累积平滑，与原始会重算历史数据的指标一致。
  - **NoRecalc**：仅对最新一根 K 线计算 T3 多项式，对应源码中的“无重算”轻量版本。
- 当 CCIT3 由正转负时开多（若开启 `Trade Overturn`，则先平空再开多）。
- 当 CCIT3 由负转正时开空（若开启 `Trade Overturn`，则先平多再开空）。
- 通过 StockSharp 的 `StartProtection` 统一管理止盈、止损与移动止损。

## 指标与计算
- **CCI**：支持 close、open、high、low、median、typical、weighted 等价格类型及自定义周期。
- **Tillson T3 平滑**：完全按照 MQL5 指标公式实现，包含 `B` 系数。Simple 模式在各阶段 EMA 中保留状态，NoRecalc 模式只根据当前 CCI 值重建多项式。
- **零轴检测**：仅在 K 线收盘后触发信号，复刻原始 EA 中的新柱判定。

## 风险与仓位管理
- `Take Profit (pts)` 与 `Stop Loss (pts)` 会根据合约的 `PriceStep` 转换为绝对价格距离。
- `Trailing Stop (pts)` 会以相同点数启动平台自带的移动止损逻辑。
- `Max Drawdown Target` 根据当前或初始资金规模调整基础手数，公式为 `volume = OrderVolume * balance / target`，设置为 0 表示保持固定手数。
- `Trade Overturn` 允许完全反手：先平掉原有仓位，再按新方向重新开仓。

## 参数
| 名称 | 默认值 | 说明 |
| --- | --- | --- |
| `Volume` | 1 | 在未启用自适应调节前的基础下单手数。 |
| `Take Profit (pts)` | 1750 | 止盈距离（点）。 |
| `Stop Loss (pts)` | 0 | 止损距离（点）。 |
| `Trailing Stop (pts)` | 0 | 移动止损距离（点，0 表示关闭）。 |
| `Trade Overturn` | false | 在反向信号出现时是否直接反手。 |
| `CCI Period` | 285 | CCI 计算周期。 |
| `CCI Price` | Typical | 参与 CCI 计算的价格类型。 |
| `T3 Period` | 60 | Tillson T3 平滑长度。 |
| `T3 Volume Factor` | 0.618 | Tillson T3 的 `B` 系数。 |
| `Mode` | Simple | CCIT3 计算模式（`Simple` 或 `NoRecalc`）。 |
| `Candle Type` | 1 小时时间框架 | 订阅并处理的 K 线周期。 |
| `Max Drawdown Target` | 0 | 用于自适应手数的资金除数（0 表示不缩放）。 |

## 实现说明
- 策略仅订阅 `Candle Type` 指定的单一 K 线源，并且只在收盘后处理信号。
- 所有下单数量都会根据合约的成交量步长调整，同时遵守 `VolumeMin` / `VolumeMax` 边界。
- 默认参数还原了原版 MT5 配置：Simple 模式、CCI 周期 285、T3 周期 60、系数 0.618。
- 切换到 NoRecalc 可保持原指标“即时”响应 CCI 符号变化，同时仍能返回正/负值以指示方向。
