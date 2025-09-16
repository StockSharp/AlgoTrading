# XRSI DeMarker Histogram 策略
[English](README.md) | [Русский](README_ru.md)

## 策略概述
该策略复刻 **Exp_XRSIDeMarker_Histogram** 专家顾问的思路。它利用一个由 RSI 和 DeMarker 组合而成的自定义振荡器来捕捉趋势反转，并对结果进行平滑处理。多空方向可以分别开启或关闭，并提供以价格步长表示的可选止损与止盈。

## 指标构成
1. **价格输入**：按照设定的周期，对所选价格类型（收盘、开盘、最高、最低、中价、典型价或加权价）计算 RSI。
2. **DeMarker 部分**：对每根完结的 K 线计算向上压力 `deMax` 和向下压力 `deMin`：
   - `deMax = max(High_t - High_{t-1}, 0)`
   - `deMin = max(Low_{t-1} - Low_t, 0)`
   两个序列均采用与 RSI 相同长度的简单移动平均进行平滑，
   - `DeMarker = deMaxAvg / (deMaxAvg + deMinAvg)`，并缩放到 0–100 区间。
3. **综合振荡器**：最终数值为 `(RSI + 100 * DeMarker) / 2`。
4. **平滑处理**：综合振荡器经由所选均线（SMA、EMA、SMMA、LWMA 或 Jurik）进行平滑。如果选择了 StockSharp 尚未实现的算法（ParMA、T3、VIDYA、AMA），系统会退回到指定周期的 EMA；Jurik 模式会应用相应的相位参数。
5. **信号历史**：保存平滑值并在 `SignalBar` 指定的历史位置上评估条件，从而模拟原始 EA 等待下一根 K 线再下单的行为。

## 交易规则
- **多头反转**
  - 条件：`SignalBar+1` 的数值低于 `SignalBar+2`（形成下降坡度），且 `SignalBar` 的数值重新抬头（`>=`）。
  - 执行：
    - 若 `CloseShortOnLongSignal` 为真，则先平掉现有空单。
    - 若 `AllowBuyEntries` 为真，在当前收盘价开多，数量为 `TradeVolume`，并在从空头翻多时加回原仓位。
- **空头反转**
  - 条件：`SignalBar+1` 的数值高于 `SignalBar+2`（形成上升坡度），且 `SignalBar` 的数值转向下行（`<=`）。
  - 执行：
    - 若 `CloseLongOnShortSignal` 为真，则先平掉现有多单。
    - 若 `AllowSellEntries` 为真，在当前收盘价开空。
- 在 RSI 与 DeMarker 尚未完成初始化之前信号会被忽略，并且只有在策略在线且允许交易时才会下单。

## 风险控制
- `StopLossTicks` 与 `TakeProfitTicks` 表示**价格步长**数量。策略将该数值乘以 `Security.PriceStep`（若未知则回退为 `1`），当 K 线的高低点触及该距离时平仓。
- 设置为 `0` 可关闭对应的止损或止盈。
- `TradeVolume` 为默认下单数量，同时在反手时用于补偿原有仓位。

## 参数说明
| 参数 | 含义 | 默认值 |
|------|------|--------|
| `TradeVolume` | 新建仓位的下单量。 | `0.1` |
| `StopLossTicks` | 以价格步长表示的止损距离。 | `1000` |
| `TakeProfitTicks` | 以价格步长表示的止盈距离。 | `2000` |
| `AllowBuyEntries` | 是否允许做多。 | `true` |
| `AllowSellEntries` | 是否允许做空。 | `true` |
| `CloseLongOnShortSignal` | 出现做空信号时是否平多。 | `true` |
| `CloseShortOnLongSignal` | 出现做多信号时是否平空。 | `true` |
| `CandleType` | 使用的 K 线周期（默认 4 小时）。 | `H4` |
| `IndicatorPeriod` | RSI 与 DeMarker 的周期。 | `14` |
| `AppliedPriceSelection` | RSI 的价格类型。 | `Close` |
| `SmoothingMethodSelection` | 平滑方法（SMA/EMA/SMMA/LWMA/Jurik/Adaptive）。 | `Sma` |
| `SmoothingLength` | 平滑均线的周期。 | `5` |
| `SmoothingPhase` | Jurik 平滑的相位。 | `15` |
| `SignalBar` | 用于判定信号的历史 K 线偏移。 | `1` |

## 与原版 EA 的差异
- 原版的资金管理模式（基于余额或可用保证金等）由直接的 `TradeVolume` 参数替代。
- 由于使用市价下单，无需 `Deviation`（滑点）设定。
- StockSharp 暂不提供 ParMA、T3、VIDYA、AMA 等平滑算法，`Adaptive` 选项会自动回退为 EMA。
- C# 源码中的注释全部采用英文，且与原 EA 一样只处理收盘完成的 K 线。
