# Color PEMA Envelopes Digit System
[English](README.md) | [Русский](README_ru.md)

**Color PEMA Envelopes Digit System** 将 MetaTrader 专家
`Exp_Color_PEMA_Envelopes_Digit_System.mq5` 迁移到 StockSharp。
策略读取 Color PEMA Envelopes 指标产生的颜色代码：当收盘价突破包络线后
指标着色，随后价格回到通道内部时，按照突破方向开仓。

## 策略流程
1. 构建八层的 PEMA（Polynomial EMA），长度可以为小数，与原指标完全一致。
   结果按照 `Digit` 参数指定的精度进行四舍五入，并可通过 `PriceShift` 做绝对平移。
2. 根据 `DeviationPercent` 在 PEMA 周围生成上下包络线。
3. 每根完成的 K 线依据其与平移后包络线的关系被赋予颜色代码：
   - `4`/`3`：收盘价高于上轨（多头/空头实体）。
   - `1`/`0`：收盘价低于下轨（多头/空头实体）。
   - `2`：价格位于通道内部。
4. 策略读取 `SignalBar + 1` 根之前的颜色并与 `SignalBar` 根之前的颜色比较，模拟原 EA 中的 `CopyBuffer` 调用。
5. 当较早的颜色显示向上突破而后一根重新回到通道内时（并且允许多头开仓），
   系统先平掉空头仓位，再开多仓。向下突破时执行对称的空头逻辑。
6. 止损和止盈距离通过 StockSharp 的保护模块自动管理。

## 参数说明
- `CandleType` – 使用的 K 线类型/周期。
- `TradeVolume` – 市价单下单数量。
- `EmaLength` – PEMA 各层 EMA 的长度（可为小数）。
- `AppliedPrice` – 计算用的价格来源（收盘、开盘、中价、加权价、TrendFollow、DeMark 等）。
- `DeviationPercent` – 包络线的百分比宽度。
- `Shift` – 计算颜色时向后偏移的已完成 K 线数量。
- `PriceShift` – 对 PEMA 的附加绝对偏移。
- `Digit` – PEMA 结果额外保留的小数位数。
- `SignalBar` – 读取当前颜色所回溯的 K 线数量（再往前一根用于“上一颜色”）。
- `AllowBuyOpen` / `AllowSellOpen` – 是否允许新的多头/空头开仓。
- `AllowBuyClose` / `AllowSellClose` – 是否允许在反向信号下平掉多头/空头。
- `StopLossPoints` – 止损距离，单位为价格点（乘以 `PriceStep`）。
- `TakeProfitPoints` – 止盈距离，单位为价格点。

## 默认值
- `CandleType = TimeSpan.FromHours(4).TimeFrame()`
- `TradeVolume = 1m`
- `EmaLength = 50.01m`
- `AppliedPrice = AppliedPrice.Close`
- `DeviationPercent = 0.1m`
- `Shift = 1`
- `PriceShift = 0m`
- `Digit = 2`
- `SignalBar = 1`
- `AllowBuyOpen = true`
- `AllowSellOpen = true`
- `AllowBuyClose = true`
- `AllowSellClose = true`
- `StopLossPoints = 1000m`
- `TakeProfitPoints = 2000m`

## 筛选信息
- **类型**：突破 / 通道回归
- **方向**：双向（多头与空头）
- **指标**：多层 PEMA 包络线
- **止损**：有（点数止损与止盈）
- **周期**：波段（默认 4 小时）
- **风险**：中等 —— 仅在价格回到通道内时建仓
- **季节性**：无
- **机器学习**：无
- **背离**：无
