# Exp XBullsBearsEyes Vol 策略

## 概述
该策略是 MetaTrader 专家顾问 **Exp_XBullsBearsEyes_Vol** 的 C# 版本。原始 EA 将 Bulls Power 与 Bears Power 指标结合，并把结果与K线成交量相乘，然后根据动量强弱对直方图着色。多头和空头各自维护两个独立的仓位槽位，以便在颜色强度增强时分批加仓。本移植版在 StockSharp 中重建了多级滤波、颜色逻辑与仓位管理，同时使用高级 API 完成下单与风险控制。

策略订阅可配置的时间框架，仅在完成的K线到来时处理数据。它重算自定义 XBullsBearsEyes 指标，并根据颜色变化决定开平仓：出现多头颜色时关闭空头仓位并可能打开一到两个多头槽位；出现空头颜色时执行相反操作。止损与止盈距离会转换为 `StartProtection` 参数，让平台风控模块管理保护性订单。

## 指标逻辑
1. 使用周期为 `IndicatorPeriod` 的 EMA 重建 Bulls Power 与 Bears Power，即比较K线高/低价与平滑后的收盘价。
2. 四阶段自适应滤波器以系数 `Gamma` 累积多头压力 (`CU`) 与空头压力 (`CD`)，指标值计算公式为 `CU / (CU + CD) * 100 - 50`。
3. 依据 `VolumeType` 选择 Tick 成交量或真实成交量，将滤波结果乘以对应成交量。
4. 乘积序列与原始成交量同时通过 `SmoothingMethod`、`SmoothingLength` 与 `SmoothingPhase`（若底层实现支持则应用 Jurik 相位）选择的移动平均进行平滑。
5. 使用 `HighLevel1`、`HighLevel2`、`LowLevel1`、`LowLevel2` 计算阈值：高于上轨输出颜色 `0` 或 `1`，低于下轨输出颜色 `3` 或 `4`，其余为中性颜色 `2`。
6. 维护颜色历史，按 `SignalBar`（默认：回看一个已完成的K线）定位信号K线，并将其颜色与上一根颜色比较以检测变化。

## 交易规则
- 颜色 `1` 与 `0` 代表多头压力。当颜色进入这两个值且上一颜色更弱时，槽位1（`PrimaryVolume`）或槽位2（`SecondaryVolume`）分别开多仓。如开启了 `AllowShortExit`，该事件同时平掉现有空头。
- 颜色 `3` 与 `4` 代表空头压力。当颜色转入这些值且上一颜色更强时，槽位1或槽位2分别开空仓。如开启了 `AllowLongExit`，该事件同时平掉现有多头。
- 每个槽位都会记录自身是否已经持仓，在仓位未被对向颜色平仓前不会重复触发加仓。
- `SignalBar` 控制在评估颜色前需要跳过多少根已完成K线（0 表示最新完成的K线）；比较颜色至少需要两条历史记录。
- `StopLossPoints` 与 `TakeProfitPoints` 以价格步长表示的点数，通过 `Security.PriceStep` 转换为绝对价格距离，并交由 `StartProtection` 管理。

## 参数说明
| 参数 | 描述 |
|------|------|
| `PrimaryVolume` | 槽位1的下单数量（对应颜色 1 / 3）。 |
| `SecondaryVolume` | 槽位2的下单数量（对应颜色 0 / 4）。 |
| `StopLossPoints` / `TakeProfitPoints` | 以最小价位为单位的止损/止盈距离，0 表示关闭。 |
| `AllowLongEntry` / `AllowShortEntry` | 是否允许在相应方向开仓。 |
| `AllowLongExit` / `AllowShortExit` | 是否允许在出现反向颜色时自动平仓。 |
| `CandleType` | 订阅与计算指标所用的时间框架（默认 8 小时）。 |
| `IndicatorPeriod` | 重建 Bulls/Bears Power 时使用的 EMA 周期。 |
| `Gamma` | 四阶段滤波器的自适应平滑系数（0.0–0.999）。 |
| `VolumeType` | 选择用于加权的 Tick 成交量或真实成交量。 |
| `HighLevel1`、`HighLevel2`、`LowLevel1`、`LowLevel2` | 决定颜色阈值的系数。 |
| `SmoothingMethod` | 平滑指标与成交量的移动平均类型（SMA、EMA、SMMA、LWMA、Jurik、JurX、ParMA→EMA、T3、VIDYA→EMA、AMA）。 |
| `SmoothingLength` | 平滑移动平均的周期。 |
| `SmoothingPhase` | Jurik 平滑的相位（限定在 [-100, 100]）。 |
| `SignalBar` | 评估颜色前需要回看的已完成K线数量。 |

## 使用建议
- 策略仅针对 `GetWorkingSecurities()` 返回的单一标的运作，所有订单均为市价单。
- 槽位管理基于净头寸：再次开仓会在现有头寸上叠加，触发平仓则一次性清空对应方向。
- 当交易所只提供 Tick 成交量时，将 `VolumeType` 设为 Real 会自动退回到 Tick 数据。
- VIDYA 与 Parabolic 平滑在移植中以指数平均近似实现，因为 StockSharp 已直接提供这些实现。
- 请确保品种的最小跳动价正确配置，从而使 `StopLossPoints` 与 `TakeProfitPoints` 转换成预期的绝对距离。
