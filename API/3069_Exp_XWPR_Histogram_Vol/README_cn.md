# Exp XWPR Histogram Vol 策略

## 概述
该策略是 MetaTrader 专家顾问 **Exp_XWPR_Histogram_Vol** 的 C# 版本。它依赖自定义指标 XWPR Histogram Vol 所产生
的颜色变化进行交易，该指标把 Williams %R 振荡器与蜡烛成交量相乘并进行平滑处理。移植版保留了原有的双仓位
资金管理机制（主仓与副仓），在 StockSharp 高层 API 中复刻相同的颜色驱动开平仓规则。

策略只处理已经结束的蜡烛。在每根新蜡烛完成时，它会回看可配置数量的历史颜色，并在颜色跨越指标定义的多空
阈值时触发操作。

## 指标逻辑
1. 将 Williams %R（`WprPeriod`）加 50 后与选定的蜡烛成交量（`VolumeMode`）相乘。
2. 加权后的 Williams %R 与原始成交量同时经过相同的平滑滤波器（`SmoothingMethod`、`SmoothingLength`、
   `SmoothingPhase`）。
3. 根据平滑后的成交量构建四条动态阈值：`HighLevel2`、`HighLevel1`、`LowLevel1`、`LowLevel2`。
4. 直方图颜色与阈值区间对应如下：
   - **0** – 直方图高于 `HighLevel2`（强势多头区域）。
   - **1** – 直方图位于 `HighLevel1` 与 `HighLevel2` 之间（温和多头）。
   - **2** – 直方图位于 `LowLevel1` 与 `HighLevel1` 之间（中性）。
   - **3** – 直方图位于 `LowLevel2` 与 `LowLevel1` 之间（温和空头）。
   - **4** – 直方图低于 `LowLevel2`（强势空头区域）。

## 信号逻辑
策略在每次评估时读取两根历史颜色：`SignalBar + 1`（较旧的颜色）以及 `SignalBar`（较新的颜色）。

- **开主仓多单（数量 = `PrimaryVolume`）**：较旧颜色为 `1`，较新颜色变为 `2`、`3` 或 `4`，同时发出平空请求。
- **开副仓多单（数量 = `SecondaryVolume`）**：较旧颜色为 `0`，较新颜色不再是 `0`，同样会平掉空头。
- **开主仓空单（数量 = `PrimaryVolume`）**：较旧颜色为 `3`，较新颜色上升到 `0`、`1` 或 `2`，并同时平掉多头。
- **开副仓空单（数量 = `SecondaryVolume`）**：较旧颜色为 `4`，较新颜色变为 `0`、`1`、`2` 或 `3`，同样强制平多。
- **平多**：较旧颜色处于 `3` 或 `4`（空头区域）。
- **平空**：较旧颜色处于 `0` 或 `1`（多头区域）。

每个方向保留两个独立的仓位槽位。只有在对应槽位处于空闲状态且启用了相应的入场开关（`AllowLongEntry`、
`AllowShortEntry`）时，信号才会触发下单。

## 风险管理
- `StopLossSteps` 与 `TakeProfitSteps` 会通过 `StartProtection` 转换为 StockSharp 的保护性订单，单位为品种的价格步长。
- `DeviationSteps` 仅为兼容原始参数而保留，对 StockSharp 的市价单没有影响。

## 参数
| 名称 | 说明 |
|------|------|
| `CandleType` | 构建指标所用蜡烛的时间框架。 |
| `PrimaryVolume`、`SecondaryVolume` | 主仓与副仓下单的数量。 |
| `AllowLongEntry`、`AllowShortEntry` | 是否允许开多 / 开空。 |
| `AllowLongExit`、`AllowShortExit` | 是否允许平多 / 平空。 |
| `StopLossSteps`、`TakeProfitSteps` | 以价格步长表示的止损与止盈，0 表示关闭该保护。 |
| `DeviationSteps` | 兼容性参数，对下单无实际作用。 |
| `SignalBar` | 信号评估的蜡烛位移（0 表示最近一根已完成蜡烛）。 |
| `WprPeriod` | Williams %R 的计算周期。 |
| `VolumeMode` | 选择直方图使用的成交量：`Tick` 为成交笔数，`Real` 为实际成交量。 |
| `HighLevel2`、`HighLevel1` | 定义多头区域的上部阈值系数。 |
| `LowLevel1`、`LowLevel2` | 定义空头区域的下部阈值系数。 |
| `SmoothingMethod` | 直方图与基准成交量所用的平滑方法。 |
| `SmoothingLength` | 平滑滤波器的长度。 |
| `SmoothingPhase` | 传递给 Jurik 类平滑器的相位参数（其它方法会忽略）。 |

## 使用说明
- 策略只交易 `GetWorkingSecurities()` 返回的单一标的，所有指令均为市价单。
- 仅在蜡烛收盘时评估信号，额外的历史缓冲可避免同一根蜡烛重复下单。
- 主仓和副仓互不干扰。可将对应数量设为 `0` 或关闭 `Allow*Entry` 开关来禁用某一槽位。
- 移植版不再使用 MetaTrader 中的 magic number 或保证金模式，仓位规模完全由 `PrimaryVolume` 与 `SecondaryVolume`
  控制。
