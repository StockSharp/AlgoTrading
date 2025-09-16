# Exp Sar Tm Plus Strategy

基于 StockSharp 高层 API 的 **Exp_Sar_Tm_Plus** 专家顾问移植版本。策略在可配置的时间框架上跟踪 Parabolic SAR 反转，并复刻原始脚本中的仓位管理与超时保护，同时与 StockSharp 的高层事件模型保持一致。

## 交易逻辑

- 根据 `CandleType` 参数订阅蜡烛数据（默认：4 小时时间框架），按 `SarStep` 与 `SarMaximum` 配置计算 Parabolic SAR。
- 每根收盘蜡烛都会缓存收盘价与 SAR 值。`SignalBar` 用于选择参与判断的已收盘蜡烛（默认是最近一根），并与上一根蜡烛比较以检测 SAR 方向是否发生改变。
- 当价格向上穿越 SAR（上一根蜡烛位于 SAR 下方，本次评估的蜡烛位于 SAR 上方）且允许做多时，策略开多头仓位；如果存在空头仓位，会在反向之前自动平掉。
- 当价格向下穿越 SAR（上一根蜡烛位于 SAR 上方，本次评估的蜡烛位于 SAR 下方）且允许做空时，策略开空头仓位；若存在多头仓位，同样会先行平掉。
- 仓位在以下任一情况发生时被关闭：SAR 反向穿越（由 `AllowLongExit` / `AllowShortExit` 控制）、命中可选的止损/止盈水平、或达到超时时间（`UseTimeExit` + `HoldingMinutes`）。
- 止损与止盈会在每次入场时根据合约的 `PriceStep` 重新计算；当对应参数为零时，相关水平不被设置。

## 参数说明

- `MoneyManagement` – 每次入场使用的基础 `Volume` 比例，≤ 0 时回退为原始 `Volume`。数值会按照品种的 `VolumeStep` 归一化。
- `ManagementMode` – 保留自原始 EA 的枚举。在该移植版中所有模式均按固定手数 (`Lot`) 处理。
- `StopLossPoints` / `TakeProfitPoints` – 以价格步长为单位的止损、止盈距离，为零时禁用。
- `DeviationPoints` – 原始脚本的滑点容忍参数。由于高层 API 直接执行市价单，此参数仅保留以便对照。
- `AllowLongEntry`、`AllowShortEntry` – 控制是否允许开仓做多/做空。
- `AllowLongExit`、`AllowShortExit` – 控制在价格与 SAR 反向交叉时是否平仓。
- `UseTimeExit` – 启用按持仓时间自动平仓。
- `HoldingMinutes` – 超时阈值（分钟）。
- `CandleType` – 用于分析 SAR 的蜡烛类型。
- `SarStep`、`SarMaximum` – Parabolic SAR 的参数。
- `SignalBar` – 信号偏移的已收盘蜡烛数量（0 表示当前收盘蜡烛，1 表示上一根，依此类推）。

## 风险管理与注意事项

- 启动时调用 `StartProtection()`，激活 StockSharp 的内置风控机制。
- 超时逻辑基于蜡烛 `CloseTime`（若缺失则回退到 `OpenTime`），以确保持仓时间统计准确。
- 策略始终保持单一净头寸，方向切换时会自动平掉相反仓位。
- 参数集合与 MQL5 版本保持一致，但某些选项（例如 `Lot` 以外的资金管理模式或 `DeviationPoints`）在高层实现中仅作为占位符。
