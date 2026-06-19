# XCCI Histogram Vol Direct 策略

## 概述
**XCCI Histogram Vol Direct Strategy** 是 MQL5 顾问 `Exp_XCCI_Histogram_Vol_Direct` 的移植版本。策略将商品通道指数（CCI）与成交量相乘，再使用可配置的移动平均对两条序列进行平滑，并观察平滑后震荡指标的斜率。当柱状图颜色翻转时，系统会先平掉逆势仓位，再按照新的方向开仓。所有计算仅在收盘后进行，因此在历史回测与实时运行中表现一致。

原始顾问使用自定义的平滑库、基于成交量的阈值以及下一根 K 线的延迟下单。StockSharp 版本保留所有输入参数，使用框架中最接近的指标进行替换，并通过高级 API 复现原有的平仓/开仓顺序。

## 适用市场与交易思路
- 适合成交量放大并伴随动量爆发的行情。
- 默认使用 2 小时 K 线，但可以根据需求调整至日内或波段周期。
- 通过观察平滑后的 CCI*Volume 斜率变化来识别动量反转。

## 指标与处理流程
1. **CCI**：在所选 `CandleType` 上计算，周期由 `CciPeriod` 控制。
2. **成交量来源**：`Tick` 或 `Real`（由于标准 K 线不提供 tick 计数，两者都使用 `candle.TotalVolume`）。
3. **加权震荡指标**：将 CCI 与所选成交量相乘。
4. **平滑处理**：使用相同的平滑器分别处理加权 CCI 和原始成交量，长度为 `SmoothingLength`：
   - `Sma` → SimpleMovingAverage
   - `Ema` → ExponentialMovingAverage
   - `Smma` → SmoothedMovingAverage
   - `Lwma` → WeightedMovingAverage
   - `Jjma` → JurikMovingAverage
   - `Jurx` → ZeroLagExponentialMovingAverage
   - `Parabolic` → ArnaudLegouxMovingAverage（`SmoothingPhase` 映射为 ALMA 的 offset）
   - `T3` → TripleExponentialMovingAverage
   - `Vidya` → ExponentialMovingAverage（最接近的替代方案）
   - `Ama` → KaufmanAdaptiveMovingAverage
5. **方向颜色**：若最新平滑值高于前一值，颜色记为 `0`（多头）；低于前一值记为 `1`（空头）；相等时延续前一个颜色，与原始缓冲区一致。
6. **信号缓存**：保存最近的颜色值，便于读取 `SignalBar` 指定的 K 线以及其上一根的颜色。

## 交易规则
### 多头管理
- **开仓**：当 `SignalBar` 的颜色变为 `1`，而上一根颜色为 `0` 时，在 `AllowLongEntries = true` 且当前无多头仓位的情况下开多单。下单数量为 `Volume + |Position|`，会先平掉现有空头。
- **平仓**：若 `SignalBar+1` 为 `0` 且 `AllowShortExits = true`，则平掉所有空头仓位，避免逆势持仓。

### 空头管理
- **开仓**：当 `SignalBar` 颜色从 `1` 变为 `0` 时，在 `AllowShortEntries = true` 且当前无空头的情况下开空单。数量规则与多头一致。
- **平仓**：若 `SignalBar+1` 为 `1` 且 `AllowLongExits = true`，则平掉所有多头仓位。

### 风险控制
- `StopLossPoints` 与 `TakeProfitPoints` 会根据 `PriceStep` 转换成价格点，通过 `StartProtection` 设置止损/止盈。
- 两个防护值默认启用，如需关闭某个保护，将其设为 `0` 即可。

## 参数速览
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CciPeriod` | CCI 周期长度。 | `14` |
| `Smoothing` | 平滑器类型。 | `T3` |
| `SmoothingLength` | 平滑长度。 | `12` |
| `SmoothingPhase` | 阶段/偏移量（仅 ALMA 使用）。 | `15` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | 原指标的阈值倍数，可用于可视化。 | `100`, `80`, `-80`, `-100` |
| `SignalBar` | 信号引用的历史 K 线（0 = 最近一根已收盘 K）。 | `1` |
| `AllowLongEntries` / `AllowShortEntries` | 是否允许开多 / 开空。 | `true` |
| `AllowLongExits` / `AllowShortExits` | 是否允许反向信号平多 / 平空。 | `true` |
| `StopLossPoints` | 止损距离（价格点）。 | `1000` |
| `TakeProfitPoints` | 止盈距离（价格点）。 | `2000` |
| `VolumeSource` | 成交量来源（`Tick`/`Real`，均使用总成交量）。 | `Tick` |
| `CandleType` | 使用的 K 线类型。 | `2 小时` |

## 每根 K 线的执行步骤
1. 等待所选周期的 K 线收盘。
2. 计算 CCI 并与成交量相乘。
3. 将加权 CCI 与成交量分别输入平滑器。
4. 当两个平滑器都完成时，计算新的颜色并更新缓存。
5. 检查 `SignalBar` 及其前一根的颜色，决定是否平仓或开新仓。
6. 应用预先设定的止损 / 止盈。

## 使用建议
- 启动策略前请设置 `Volume`（单笔交易量），默认值为 0 时不会下单。
- 由于标准 K 线缺少 tick 数，两种 `VolumeSource` 实际上使用同一字段；如需 tick 级数据，可自定义 K 线。
- `SmoothingPhase` 仅影响 ALMA，其它平滑器会忽略该参数。
- 阈值倍数保留自原始指标，可在图表中叠加平滑成交量后自行绘制。

## 与 MQL5 版本的差异
- StockSharp 中没有 VIDYA 与 Parabolic MA 的直接实现，使用 EMA 与 ALMA 作为近似替代，反应特性可能略有差异。
- 本策略在信号 K 线收盘时立即下单。原始顾问通过 `TimeShiftSec` 在下一根 K 线开始时下单，两者在低延迟市场中效果接近。
- tick 成交量以总成交量近似，因为标准 K 线消息不包含 tick 计数。

## 入门步骤
1. 选择标的并设置策略属性中的 `Volume`。
2. 根据交易计划调整 `CandleType`、`CciPeriod`、`Smoothing*` 等参数。
3. 配置止损/止盈距离，或将其设为 0 以禁用。
4. 建议先在模拟/回测环境中观察平滑指标曲线，再上线实盘。

## 优化方向
- 同时优化 `CciPeriod` 与 `SmoothingLength`，以匹配目标市场的节奏。
- 将 `SignalBar` 设置为 0 与 1，比较提前或延迟确认的差异。
- 根据 ATR 或历史波动调整 `StopLossPoints` 与 `TakeProfitPoints`。
- 可叠加更高周期的趋势过滤器，以减少逆势交易。

## 风险清单
- 上线前确认 `PriceStep`、`Volume` 与交易标的规格一致。
- 监控滑点与成交延迟，必要时添加额外的风控措施。
- 定期检查日志，确保 `Allow*` 参数符合当前的交易假设。
