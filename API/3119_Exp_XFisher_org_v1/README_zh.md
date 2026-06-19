# Exp XFisher org v1 策略

## 概述
本策略重现了 MetaTrader 5 专家顾问 **Exp_XFisher_org_v1**。它基于 Fisher 变换寻找价格反转，并使用可配置的移动平均线对
结果进行二次平滑，从而保持原版逆势交易的特性：当 Fisher 曲线在上升后转为向下时开多；当曲线在下行后转为向上时开空；
一旦指标朝相反方向翻转便立即平掉当前仓位。

辅助指标 `XFisherOrgIndicator` 位于 `CS/ExpXFisherOrgV1Strategy.cs` 中，完全按照 MT5 的实现步骤计算：

1. 在最近 `Length` 根已完成的 K 线中寻找最高价和最低价；
2. 将所选价格源（见下方“应用价格”）使用上述极值归一化到 0–1 区间；
3. 应用递归滤波公式 `value = (wpr - 0.5) + 0.67 * value[prev]`，随后套用 Fisher 变换
   `fish = 0.5 * ln((1 + value) / (1 - value)) + 0.5 * fish[prev]`；
4. 对结果使用指定的移动平均线进行平滑。平滑后的值作为主线，信号线则是主线向后平移一个 bar，与 MQL 中
   “Buffer #1 保存前一根值”的做法一致。

移植版本保持了 MT5 默认参数（`Length = 7`，Jurik 平滑长度 5、相位 15，H4 周期），并提供与原版相同的多空开平仓开关。

## 交易逻辑
- **做多**：当 `SignalBar + 1` 根之前的 Fisher 值仍在上升（`Fisher[SignalBar+1] > Fisher[SignalBar+2]`）而
  `SignalBar` 对应的值下穿或触及其延迟副本（`Fisher[SignalBar] <= Fisher[SignalBar+1]`）时开多；
- **做空**：当 `SignalBar + 1` 根之前的 Fisher 值仍在下降，而 `SignalBar` 对应的值上穿其延迟副本时开空；
- **平仓**：相反方向的拐点会优先触发平仓，再决定是否建立新的仓位；
- **下单手数**：由 `OrderVolume` 控制。如需反向持仓，会一次性发送足够的市价单来平掉旧仓并同时建立新仓，
  以模拟原脚本 `BuyPositionOpen` / `SellPositionOpen` 的行为。

所有计算均基于**已收盘的 K 线**。当 `SignalBar = 0` 时直接使用最新收盘的蜡烛；大于 0 时按照 MT5 的定义向后偏移同样的
数量。

## 参数
| 名称 | 说明 | 默认值 |
| ---- | ---- | ------ |
| `OrderVolume` | 每笔市价单的数量。 | `1` |
| `BuyOpenAllowed` / `SellOpenAllowed` | 是否允许开多 / 开空。 | `true` |
| `BuyCloseAllowed` / `SellCloseAllowed` | 是否允许平多 / 平空。 | `true` |
| `SignalBar` | 读取 Fisher 缓冲的偏移量（单位：已收盘 K 线数）。 | `1` |
| `Length` | 最高 / 最低价的回溯周期。 | `7` |
| `SmoothingLength` | 平滑平均线的周期。 | `5` |
| `Phase` | Jurik 平滑的相位参数（其他方法忽略）。 | `15` |
| `SmoothingMethod` | 施加于 Fisher 输出的平滑方法。 | `Jjma` |
| `PriceType` | 指标使用的价格源（收盘价、开盘价、中值等）。 | `Close` |
| `CandleType` | 计算所用的 K 线类型（默认 4 小时）。 | `H4` |

## 平滑方法映射
原始指标提供了大量自定义滤波器。StockSharp 版本将其映射到稳定的库内实现：

- `Jjma`、`Jurx`、`T3` → `JurikMovingAverage`（若库暴露 `Phase` 属性则写入相位值）。
- `Sma`、`Ema`、`Smma`、`Lwma` → 对应的 StockSharp 移动平均线。
- `Parabolic` → 使用 `ExponentialMovingAverage` 近似（在 StockSharp 中表现最接近）。
- `Vidya`、`Ama` → `KaufmanAdaptiveMovingAverage`（利用 Kaufman AMA 模拟 VIDYA/AMA 的自适应特性）。

这种映射方式与仓库中其他 Kositsin 指标移植保持一致，使平滑后的 Fisher 曲线响应尽量贴近原版。

## 与 MT5 专家顾问的差异
- **资金管理**：StockSharp 直接使用显式下单量，原脚本的 `MM` / `MarginMode` 参数被合并为单一的 `OrderVolume` 设置。
- **执行模型**：策略通过高阶订阅接口在每根收盘 K 线上评估信号，不再逐 tick 轮询，因此无需原有的 `IsNewBar` 辅助类，
  也能避免重复下单。
- **应用价格**：保留了 `SmoothAlgorithms.mqh` 中的全部选项，包括 TrendFollow 和 Demark 价格。
- **图表展示**：默认绘制蜡烛图、平滑后的 Fisher 线以及实际成交点位。

## 文件结构
- `CS/ExpXFisherOrgV1Strategy.cs` —— 策略主体、XFisher 指标及其输出容器。

