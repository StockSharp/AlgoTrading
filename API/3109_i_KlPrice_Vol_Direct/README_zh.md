# Exp i-KlPrice Vol Direct 策略

## 概述
**Exp i-KlPrice Vol Direct** 是 MetaTrader 5 专家顾问 `Exp_i-KlPrice_Vol_Direct` 的 StockSharp 版本。原始系统会构建 KlPrice 指标、乘以成交量、经过多层平滑，然后根据平滑曲线的斜率变化执行交易。移植版本保留了整条处理链，暴露相同的输入参数，并在每根 K 线收盘后通过 StockSharp 高级 API 下单。

保留下来的核心思想：
- **价格与波动区间的双重平滑**：价格数据由可配置的移动平均线处理，`High-Low` 区间单独平滑，形成自适应的动态带。
- **成交量加权**：振荡器输出在进入最终 Jurik 滤波器前乘以所选的成交量流（勾数或实际成交量），强化伴随放量的行情。
- **颜色状态**：策略跟踪平滑后振荡器的斜率。颜色从空头切换到多头时平掉空单并开多，反向切换时平掉多单并开空。
- **信号延迟**：`SignalBar` 参数允许在执行前等待额外的已收盘 K 线，与原始 EA 的确认逻辑一致。

## 计算流程
1. **选择价格类型**：支持原指标的 12 种价格公式（Close、Open、Median、Demark、TrendFollow 等）。
2. **第一阶段平滑**：对价格序列应用 `PriceMethod`，长度为 `PriceLength`，`PricePhase` 仅在 Jurik 平滑时生效。SmoothAlgorithms 中的算法被映射到 StockSharp 指标：
   - `Sma` → `SimpleMovingAverage`
   - `Ema` → `ExponentialMovingAverage`
   - `Smma` → `SmoothedMovingAverage`
   - `Lwma` → `WeightedMovingAverage`
   - `Jjma` → `JurikMovingAverage`（若提供 `Phase` 属性则写入）
   - `Jurx` → `ZeroLagExponentialMovingAverage`
   - `Parma` → `ArnaudLegouxMovingAverage`（将 `phase` 转为 ALMA 偏移量）
   - `T3` → `TripleExponentialMovingAverage`
   - `Vidya` → 以 `ExponentialMovingAverage` 近似
   - `Ama` → `KaufmanAdaptiveMovingAverage`
3. **区间平滑**：对 `High-Low` 区间重复相同的过程，使用 `RangeMethod`、`RangeLength`、`RangePhase`，得到自适应的波动范围。
4. **构建振荡器**：按照 `(Price - (PriceMA - RangeMA)) / (2 * RangeMA) * 100 - 50` 计算，与 MQL 公式完全一致，并乘以所选的成交量流 (`VolumeSource`)。
5. **最终 Jurik 平滑**：将加权振荡器和原始成交量同时输入 Jurik 移动平均，长度为 `ResultLength`（相位固定为 100，与原脚本一致）。
6. **颜色判定**：比较当前与前一个平滑值。上升标记为 0（多头），下降标记为 1（空头），相等时继承上一颜色。颜色按时间顺序存储，方便 `SignalBar` 控制延迟。

## 交易规则
### 多头模块
- **开仓**：如果 `SignalBar` 指定的 K 线颜色为多头 (`0`)，而上一根颜色为空头 (`1`)，在 `AllowLongEntries = true` 且当前净仓位非正时买入。委托量取 `Volume + |Position|`，自动平掉反向仓位。
- **平仓**：当信号颜色为多头且 `AllowShortExits = true` 时，立即平掉所有空头仓位。

### 空头模块
- **开仓**：若信号颜色变为空头 (`1`)，上一颜色为多头 (`0`)，在 `AllowShortEntries = true` 且净仓位非负时卖出。
- **平仓**：当信号颜色为空头且 `AllowLongExits = true` 时，关闭多头仓位。

策略仅在 K 线收盘后执行交易。仓位大小由基础属性 `Strategy.Volume` 决定；原 EA 的资金管理模式未移植。

## 参数说明
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 分析所用的 K 线时间框架。 | `H4` |
| `VolumeSource` | 用于加权的成交量（`Tick` 或 `Real`，在本实现中都引用 `candle.TotalVolume`）。 | `Tick` |
| `PriceMethod` / `PriceLength` / `PricePhase` | 价格平滑方式、周期及 Jurik 相位。 | `Sma`, `100`, `15` |
| `RangeMethod` / `RangeLength` / `RangePhase` | 区间平滑方式、周期及相位。 | `Jjma`, `20`, `100` |
| `ResultLength` | 最终 Jurik 滤波器的周期（同样作用于成交量）。 | `20` |
| `PriceMode` | 价格类型（Close、Open、Median、Demark、TrendFollow0/1 等）。 | `Close` |
| `HighLevel2`, `HighLevel1`, `LowLevel1`, `LowLevel2` | 预留的阈值倍数，主要用于诊断或可视化，不影响信号。 | `0`, `0`, `0`, `0` |
| `SignalBar` | 评估信号前需跳过的已收盘 K 线数量。 | `1` |
| `AllowLongEntries` / `AllowShortEntries` | 是否允许开多 / 开空。 | `true` |
| `AllowLongExits` / `AllowShortExits` | 是否允许在颜色反向时平掉多 / 空。 | `true` |
| `StopLossPoints` / `TakeProfitPoints` | 止损与止盈的点值（乘以 `PriceStep` 传入 `StartProtection`）。 | `1000`, `2000` |

## 风险控制
- 止损与止盈会转为 `UnitTypes.Point`，并由 `StartProtection` 自动维护。设为 0 可关闭对应保护。
- 委托手数完全由 `Volume` 决定，请确保与合约规格匹配。
- 仅在 `IsFormedAndOnlineAndAllowTrading()` 返回真时执行交易逻辑，避免在数据或连接未准备好时下单。

## 与 MQL5 版本的差异
- **平滑算法映射**：SMA、EMA、SMMA、LWMA、Jurik、KAMA 与原版完全对应；`Jurx`、`Parma`、`Vidya` 使用 ZeroLag EMA、ALMA、EMA 近似，某些组合可能与 MT5 略有差异。
- **成交量数据**：标准 K 线只提供总成交量，如需真实勾数请构造自定义 K 线并将值写入 `TotalVolume`。
- **资金管理**：未实现原脚本的 `MM` 与 `MarginMode`。请使用 `Volume` 或外部模块管理仓位大小。
- **执行时机**：下单发生在信号 K 线收盘后，而不是通过 `TimeShiftSec` 安排下一根开盘；对市价单而言结果等效。

## 使用建议
1. 将策略附加到目标标的，设置 `Volume` 并确认 `Security.PriceStep` 正确。
2. 通过 `CandleType` 选择时间框架，策略只订阅这一档 K 线。
3. 调整平滑方法与周期，在图表上验证指标形态（`DrawCandles`、`DrawIndicator` 会自动绘制）。
4. 根据容忍的确认级别设置 `SignalBar`：0 代表最快，≥1 可以降低噪音。
5. 先在模拟或回测环境评估风险参数，再部署到真实账户。

## 优化方向
- 联合优化 `PriceLength`、`RangeLength`、`ResultLength`，在响应速度与平滑度之间取得平衡。
- 测试不同的 `PriceMode`，寻找对目标市场最敏感的价格类型。
- 通过调整 `SignalBar` 限制横盘震荡中的虚假反转信号。
- 将 `HighLevel*` / `LowLevel*` 作为自定义图层或统计指标使用。

## 安全检查
- 确认账户允许指定的交易手数，并且经纪商能正确解释所选的成交量模式。
- 关注成交延迟；策略假设市价单能在收盘附近成交。
- 升级 StockSharp 或调整参数后，定期核对颜色变化日志，确保平滑近似仍符合预期。
