# Exp XBullsBearsEyes Vol Direct 策略

## 概述
该策略是 MetaTrader 专家顾问 **Exp_XBullsBearsEyes_Vol_Direct** 的 C# 版本。它重建了由 Bulls Power 与 Bears Power
组成的自定义振荡器，将结果与可配置的成交量源相乘，并应用与原版相同的四阶 Gamma 滤波。交易决策完全
取决于指标的方向缓冲区：策略捕捉平滑直方图的斜率变化，而不是水平突破，从而在动量转折点开仓或
平仓。

与许多移植版本不同，StockSharp 版本保留了对成交量的加权处理以及原始 MQL 代码中使用的四级 `Gamma`
滤波链。直方图与原始成交量分别采用相同的移动平均类型进行二次平滑，仅当两条序列都完全形成后才会
产生交易信号。策略只处理已完成的蜡烛，可在支持成交量或仅提供 tick 数的市场之间切换，适用性更强。

## 指标逻辑
1. 使用长度为 `Period` 的收盘价指数移动平均线计算 Bulls Power 和 Bears Power。
2. 将两个力量值输入四级 Gamma 滤波器 (`L0`–`L3`)，得到归一化直方图（范围 -50 至 +50）。
3. 将直方图乘以所选的成交量源（tick 数或真实成交量）。
4. 使用同一种移动平均方法 (`Method`, `SmoothingLength`, `SmoothingPhase`) 分别平滑直方图与成交量序列。
5. 构造方向缓冲区：当平滑直方图上升时方向为 `0`，下降时为 `1`，等价于 MetaTrader 版本中的
   `ColorDirectBuffer`。

阈值缓冲区（HighLevel/LowLevel）也会在内部计算以保持兼容，但不会影响交易过滤，与原始专家顾问的行为
一致。

## 交易规则
- **平掉空头**：若前一根柱子的方向为多头（`olderColor = 0`）。
- **开多头**：在允许做多的情况下，若前一柱为多头而当前柱转为空头（`currentColor = 1`）且当前无多单。
- **平掉多头**：若前一根柱子的方向为空头（`olderColor = 1`）。
- **开空头**：在允许做空的情况下，若前一柱为空头而当前柱转为多头（`currentColor = 0`）且当前无多单。
- 当需要反向时，先平掉对冲头寸，再按 `OrderVolume` 提交新的市价单。

信号读取支持柱子位移 (`SignalBar`)，默认值为 `1`，与原始 MQL 程序一样会等待蜡烛完全收盘后再执行操作。

## 参数
| 名称 | 说明 |
|------|------|
| `CandleType` | 策略订阅的蜡烛类型/周期（默认两小时）。 |
| `Period` | 计算 Bulls/Bears Power 的窗口长度。 |
| `Gamma` | Gamma 滤波器的平滑系数（0…1）。 |
| `VolumeMode` | 成交量来源：tick 数或真实成交量。 |
| `Method` | 直方图与成交量使用的移动平均类型（SMA、EMA、SMMA、LWMA、Jurik；不支持的旧方法会退回到 SMA）。 |
| `SmoothingLength` | 两个平滑阶段的长度。 |
| `SmoothingPhase` | Jurik 平滑的相位参数（用于兼容）。 |
| `SignalBar` | 读取方向缓冲区时回溯的柱子数。 |
| `AllowBuyOpen` / `AllowSellOpen` | 是否允许开多/开空。 |
| `AllowBuyClose` / `AllowSellClose` | 是否允许在反向信号出现时强制平仓。 |
| `OrderVolume` | 新开仓市价单的手数/数量。 |
| `StopLossPoints` | 以最小价格步长表示的止损距离（0 表示禁用）。 |
| `TakeProfitPoints` | 以最小价格步长表示的止盈距离（0 表示禁用）。 |

## 使用建议
- 策略仅针对 `GetWorkingSecurities()` 返回的单一标的运行，建议用于成交量分布较稳定的市场。
- 对于外汇等仅提供 tick 数的市场，请选择 `VolumeMode = Tick`；若交易所能提供真实成交量，则可切换到
  `VolumeMode = Real`。
- 止损和止盈距离以价格步长计量，策略会自动乘以 `PriceStep` 转换为绝对价格。
- 当平滑直方图保持不变时，方向缓冲区会沿用上一颜色，完全复刻 MetaTrader 的处理方式。
- 默认仅绘制价格蜡烛，如需可视化直方图，可自行扩展图表绘制代码。
