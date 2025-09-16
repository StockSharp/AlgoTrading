# ATR Normalize Histogram 策略

## 概述
ATR Normalize Histogram 策略在 StockSharp 中还原了 MetaTrader 专家顾问 *Exp_ATR_Normalize_Histogram* 的逻辑。系统跟踪经平滑处理的收盘价到最低价差与经平滑处理的真实波动区间的比值。当指标颜色发生变化时触发入场或出场，这与原始 MQL5 程序的多缓冲区实现保持一致。

## 指标计算
1. 每根完成的蜡烛都会计算：
   - `diff = 收盘价 − 最低价`；
   - `range = max(最高价, 前一根收盘价) − min(最低价, 前一根收盘价)`。
2. 两个序列分别使用可选的平滑方法与周期进行处理。提供五种方法：Simple、Exponential、Smoothed (RMA)、Weighted、Jurik。当选择原版中的 JurX、Parabolic、T3、VIDYA、AMA 等方法时，会退回到简单移动平均。
3. 归一化柱状值按照以下公式计算：

   `normalized = 100 × smoothedDiff / max(|smoothedRange|, PriceStep)`。
4. 根据阈值将柱状值划分为五个区间，以模拟指标颜色缓冲区。

## 交易逻辑
- **入场** —— `SignalBar` 指定需要评估的历史柱（默认 1，表示上一根已完成的蜡烛）。策略比较该柱与其前一根柱的颜色：
  - 颜色从看涨极值 (`0`) 变为其他值时，如果允许做多则开多单；
  - 颜色从看跌极值 (`4`) 变为其他值时，如果允许做空则开空单。
- **出场** —— 仅上一根柱的颜色就可以触发平仓：
  - 颜色为 `0` 时（看涨极值）关闭空头仓位；
  - 颜色为 `4` 时（看跌极值）关闭多头仓位。
- 出场逻辑总是在新的入场信号之前执行，避免同时持有多个方向的仓位。

## 风险管理
策略记录最近一次成交价，并可选地应用以“点”为单位的止损与止盈。转换使用 `Security.PriceStep`，与原专家顾问中的 point 概念一致。当价格在当前柱内触及任一限制时立即平仓，随后可根据下一次信号重新建仓。

## 参数
- `CandleType` —— 指标所用的时间框架。
- `FirstSmoothingMethod` / `SecondSmoothingMethod` —— 分别用于 `diff` 与 `range` 的平滑类型。
- `FirstLength` / `SecondLength` —— 平滑周期。
- `HighLevel`、`MiddleLevel`、`LowLevel` —— 柱状阈值（默认 60/50/40）。
- `SignalBar` —— 缓冲区偏移量（最小值 1）。
- `EnableBuyEntries`、`EnableSellEntries`、`EnableBuyExits`、`EnableSellExits` —— 各方向的开仓与平仓开关。
- `TradeVolume` —— 基础下单数量，若需要反向会自动叠加对冲数量。
- `StopLossPoints`、`TakeProfitPoints` —— 以点为单位的止损与止盈，设为 0 表示禁用。

## 与 MQL 版本的差异
- 两个平滑阶段可以分别配置，但仅提供五种 StockSharp 内置移动平均。选择其他方法时自动使用简单移动平均并保留周期设定。
- `SignalBar` 的实现与 `CopyBuffer` 的偏移规则一致，即使使用更大的偏移量，也始终比较该柱与其前一根柱的颜色。
- 原始专家顾问中的资金管理参数（`MM`、`MMMode`、`Deviation`）被简化为单一的 `TradeVolume`。下单使用市价，同时可启用可选的止损/止盈监控。
