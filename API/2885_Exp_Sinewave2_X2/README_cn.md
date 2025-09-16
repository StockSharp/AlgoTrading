# Exp Sinewave2 X2 策略

## 概述
Exp Sinewave2 X2 是基于 John Ehlers Sinewave 理论的多周期趋势策略。高周期用于判定主要趋势，低周期提供精确的入场和离场。所有信号都来自重写的 Sinewave2 指标，该指标内部调用自适应的 CyclePeriod 组件以保持与原始 MQL 版本一致。

## 指标
- **高周期 Sinewave2（领先线与正弦线）**：通过领先线与主线的关系判断牛市或熊市背景。
- **低周期 Sinewave2**：跟踪最新的交叉，用于发出与高周期一致的交易指令。

## 交易逻辑
1. **趋势过滤**
   - 在高周期上计算 Sinewave2。
   - 读取 `SignalBarHigh` 根之前的领先线与正弦线数值。
   - 若 `Lead > Sine` 判定为多头趋势，`Lead < Sine` 判定为空头，否则视为中性。
2. **入场条件**
   - 等待低周期蜡烛收盘。
   - 按 `SignalBarLow` 与 `SignalBarLow + 1` 的偏移读取低周期领先线与正弦线。
   - 多头入场：上一根为向下交叉（领先线高于主线，当前回落到主线之下），同时高周期趋势为多头并启用 `EnableBuyOpen`。
   - 空头入场：上一根为向上交叉（领先线低于主线，当前突破到主线上方），同时高周期趋势为空头并启用 `EnableSellOpen`。
3. **离场规则**
   - `EnableBuyCloseLower` / `EnableSellCloseLower` 控制低周期的反向交叉是否触发平仓。
   - `EnableBuyCloseTrend` / `EnableSellCloseTrend` 在高周期趋势翻转时立即强制平仓。
   - 止损与止盈按 `StopLossPoints` / `TakeProfitPoints` 所定义的点数距离，结合蜡烛的最高价和最低价逐根检查。
4. **风险管理**
   - 反向开仓时下单量为 `Volume + |Position|`，以便同时平掉旧仓并建立新仓。
   - 每次入场后调用 `SetRiskLevels`，根据 `Security.PriceStep` 计算绝对的止损和止盈价格（若不可用则退化为 1）。

## 参数
| 名称 | 说明 |
| --- | --- |
| `AlphaHigh` | 高周期 Sinewave2 的 Alpha 平滑系数。 |
| `AlphaLow` | 低周期 Sinewave2 的 Alpha 平滑系数。 |
| `SignalBarHigh` | 读取高周期趋势时回溯的蜡烛数量。 |
| `SignalBarLow` | 读取低周期交叉时回溯的蜡烛数量。 |
| `EnableBuyOpen` / `EnableSellOpen` | 是否允许低周期信号开多/开空。 |
| `EnableBuyCloseTrend` / `EnableSellCloseTrend` | 当高周期趋势反转时是否强制平仓。 |
| `EnableBuyCloseLower` / `EnableSellCloseLower` | 低周期出现反向交叉时是否平仓。 |
| `StopLossPoints` | 以价格步长表示的止损距离。 |
| `TakeProfitPoints` | 以价格步长表示的止盈距离。 |
| `HigherCandleType` / `LowerCandleType` | 用于过滤和触发的蜡烛数据类型（周期）。 |

## 说明
- 策略仅处理已完成的蜡烛，忽略未收盘数据。
- Sinewave2 与 CyclePeriod 的实现完全遵循 Ehlers 原版算法，以确保行为与 MQL 脚本一致。
- 当高低周期相同时时，共用同一蜡烛订阅以避免重复请求。
- 在实盘或回测前，可通过基础 `Strategy` 的 `Volume` 属性调整每笔交易的基准手数。
