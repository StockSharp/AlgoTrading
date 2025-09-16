# XFatl XSatl Cloud 逆势策略
[English](README.md) | [Русский](README_ru.md)

本策略在 StockSharp 中复刻了 MT5 专家顾问 **Exp_XFatlXSatlCloud**。它跟踪经过平滑处理的 FATL/SATL 云图，并在交叉发生后执行**逆势**交易：当快速线（XFATL）先位于慢速线（XSATL）之上、随后回落到其下方时开多；当快速线先位于慢速线之下、随后重新上穿时开空。止盈与止损使用合约的最小价格步长表示。

## 交易逻辑

- 默认时间框架为 8 小时，可通过 `CandleType` 参数选择其他蜡烛类型。
- 两条云线由 StockSharp 的移动平均组合构成。默认使用 Jurik 均线，可自定义长度与相位；同时提供 SMA、EMA、SMMA、WMA 等替代方案。
- `SignalBar` 参数控制信号所使用的历史蜡烛（相对于最新收盘柱的偏移）。策略维护一段短历史序列，从而精确复现 MT5 中“当前值 vs. 前一个值”的比较方式。
- 入场规则（逆势）：
  - **做多**：上一根柱子快速线高于慢速线，而当前柱子回落到慢速线之下或持平。
  - **做空**：上一根柱子快速线低于慢速线，而当前柱子上穿慢速线或与之持平。
- 离场规则：
  - 当上一根柱子呈现空头云（快速线低于慢速线）且 `AllowLongExit` 启用时，平掉现有多单。
  - 当上一根柱子呈现多头云（快速线高于慢速线）且 `AllowShortExit` 启用时，平掉现有空单。
- 在旧仓位完全平仓之前不会建立新仓，保持与原始 MT5 策略相同的节奏。

## 风险控制

- `TradeVolume` 决定每次下单的数量，策略不会分批加仓。
- `TakeProfitTicks` 与 `StopLossTicks` 会转换为价格步长，交由 `StartProtection` 管理。设置为 0 即可关闭相应的保护单。
- MT5 版本依赖于经纪商参数计算手数，本移植版改为直接控制下单量与止盈止损距离。

## 参数说明

| 参数 | 说明 |
|------|------|
| `CandleType` | 计算指标所用的蜡烛类型或时间框架。 |
| `FastMethod` / `SlowMethod` | XFATL 与 XSATL 所使用的平滑算法（默认 Jurik）。 |
| `FastLength` / `SlowLength` | 快速线与慢速线的周期。 |
| `FastPhase` / `SlowPhase` | Jurik 均线的相位参数（若指标支持）。 |
| `SignalBar` | 评估信号时所使用的历史偏移（1 = 前一根已完成蜡烛）。 |
| `TradeVolume` | 每次建仓的数量。 |
| `AllowLongEntry` / `AllowShortEntry` | 是否允许做多 / 做空的逆势入场。 |
| `AllowLongExit` / `AllowShortExit` | 是否允许根据相反信号平掉现有多单 / 空单。 |
| `TakeProfitTicks` | 止盈距离（单位：价格步长）。 |
| `StopLossTicks` | 止损距离（单位：价格步长）。 |

## 实现细节

- 仅保存满足 `SignalBar` 需要的少量指标历史值，避免额外的数据缓冲区。
- 通过反射设置 Jurik 指标的相位参数，以兼容不同版本的 StockSharp；若指标不支持该属性则忽略。
- 当前实现使用蜡烛收盘价作为输入，与多数原始 EA 配置一致。如需其它价格类型需进一步扩展代码。
- 整个策略使用高层 API（`SubscribeCandles`、`Bind`、`StartProtection`），方便在 Designer、Runner 等 StockSharp 产品中直接复用。
