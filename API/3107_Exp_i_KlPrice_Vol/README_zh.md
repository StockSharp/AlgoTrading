# Exp i-KlPrice Vol 策略

## 概述
该策略是 MetaTrader 专家 **Exp_i-KlPrice_Vol.mq5** 的 C# 版本。策略重新实现 KlPrice 振荡器：它衡量价格相对于波动带的位置，将结果乘以蜡烛成交量，并根据自适应阈值的颜色切换生成信号。每个方向都包含两个独立的持仓槽位，以模拟原始 EA 中两个 magic 编号的行为。

## 指标逻辑
- 根据 `AppliedPrice` 设定（收盘价、开盘价、中值、Demark 等）转换蜡烛价格。
- 使用 `PriceMaMethod` 与 `PriceMaLength` 定义的移动平均对转换后的价格进行平滑。
- 蜡烛振幅 (`High - Low`) 通过 `RangeMaMethod`/`RangeMaLength` 进行平滑，用作动态通道宽度。
- 基础 KlPrice 振荡器按公式 `100 * (Price - (MA - RangeMA)) / (2 * RangeMA) - 50` 计算。
- 将振荡器乘以所选的体量源（`AppliedVolume.Tick` 或 `AppliedVolume.Real`）。
- 长度为 `SmoothingLength` 的 Jurik 平滑同时作用于振荡器和体量，得到两条自适应序列。
- 将平滑后的体量分别乘以 `HighLevel2`、`HighLevel1`、`LowLevel1`、`LowLevel2`，得到自适应阈值。
- 振荡器颜色由平滑值与阈值比较得出：
  - **4** – 高于 `HighLevel2 * volume`，表明强势多头。
  - **3** – 介于上限和极值之间。
  - **2** – 上下阈值之间的中性区。
  - **1** – 介于下阈值与零轴之间。
  - **0** – 低于 `LowLevel2 * volume`，表明强势空头。

## 交易规则
1. 读取 `SignalBar` 指定的历史蜡烛颜色（通常是上一根已完成的蜡烛）以及再早一根的颜色。
2. 多头开仓：
   - 槽位 1：颜色从 **4** 下降到 **4** 以下且 `AllowLongEntry` 为 `true` 时触发。
   - 槽位 2：颜色从 **3** 下降到 **3** 以下时触发。
3. 空头开仓：
   - 槽位 1：颜色从 **0** 上升到 **0** 以上且 `AllowShortEntry` 为 `true` 时触发。
   - 槽位 2：颜色从 **1** 上升到 **1** 以上时触发。
4. 多头平仓：若较早的颜色为 **0** 或 **1**，并且 `AllowLongExit` 已启用，则关闭全部多头头寸。
5. 空头平仓：若较早的颜色为 **4** 或 **3**，并且 `AllowShortExit` 已启用，则关闭全部空头头寸。
6. 每个槽位都会记录最近的信号时间，防止在同一根蜡烛上重复下单。当 `StopLossPoints` 或 `TakeProfitPoints` 大于零时，通过 `StartProtection` 启用止损/止盈保护。

## 参数
| 名称 | 类型 | 默认值 | 说明 |
|------|------|--------|------|
| `PrimaryVolume` | `decimal` | `0.1` | 第一槽位的下单量。 |
| `SecondaryVolume` | `decimal` | `0.2` | 第二槽位的下单量。 |
| `StopLossPoints` | `int` | `1000` | 止损距离（以最小价位计）。 |
| `TakeProfitPoints` | `int` | `2000` | 止盈距离（以最小价位计）。 |
| `AllowLongEntry` | `bool` | `true` | 是否允许开多。 |
| `AllowShortEntry` | `bool` | `true` | 是否允许开空。 |
| `AllowLongExit` | `bool` | `true` | 是否在出现空头颜色时平多。 |
| `AllowShortExit` | `bool` | `true` | 是否在出现多头颜色时平空。 |
| `CandleType` | `DataType` | `H8` | 指标使用的蜡烛时间框架。 |
| `PriceMaMethod` | `SmoothMethod` | `Sma` | 价格平滑所用的移动平均类型。 |
| `PriceMaLength` | `int` | `100` | 价格平滑周期。 |
| `PriceMaPhase` | `int` | `15` | Jurik 滤波器的相位参数。 |
| `RangeMaMethod` | `SmoothMethod` | `Jjma` | 蜡烛振幅的平滑方法。 |
| `RangeMaLength` | `int` | `20` | 振幅平滑周期。 |
| `RangeMaPhase` | `int` | `100` | 振幅平滑的相位参数。 |
| `SmoothingLength` | `int` | `20` | 对振荡器和体量执行 Jurik 平滑的长度。 |
| `AppliedPrice` | `AppliedPrice` | `Close` | 振荡器计算所用的价格来源。 |
| `VolumeType` | `AppliedVolume` | `Tick` | 乘以振荡器的体量来源。 |
| `HighLevel2` | `int` | `150` | 上方极值阈值系数。 |
| `HighLevel1` | `int` | `20` | 上方中间阈值系数。 |
| `LowLevel1` | `int` | `-20` | 下方中间阈值系数。 |
| `LowLevel2` | `int` | `-150` | 下方极值阈值系数。 |
| `SignalBar` | `int` | `1` | 读取颜色所用的历史偏移。 |

## 使用说明
- 建议选择同时提供价格与成交量数据的品种；若缺乏真实成交量，则使用蜡烛的 tick 数作为替代。
- 通过分别调整 `PrimaryVolume` 和 `SecondaryVolume`，可模拟原始 EA 中两个资金管理通道。
- 当需要避开未完成蜡烛或重新同步历史数据时，可修改 `SignalBar`。
- 平滑方法通过反射支持 Jurik 滤波器，以尽可能贴近 MQL `SmoothAlgorithms` 库的行为。
- 只有在 `StopLossPoints` 或 `TakeProfitPoints` 为正时才会启动保护；将两者设为零可完全关闭自动止损/止盈。
