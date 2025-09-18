# JBrainTrend1Stop 策略

**JBrainTrend1Stop** 策略是 MetaTrader 5 专家顾问 `Exp_JBrainTrend1Stop` 在 StockSharp 平台上的移植版本。它将两条 Average True Range、Stochastic 振荡器以及三条 Jurik 移动平均线结合起来，用于识别 BrainTrading 趋势反转。当 Jurik 平滑后的价格出现足够大的摆动并且 Stochastic 脱离中性区间时，策略会切换方向、更新 BrainTrend 止损线，并且（在可选的情况下）在指定的延迟后反转净持仓。

## 交易逻辑

1. 订阅 `CandleType` 指定的K线，并将其输入以下指标：
   - 主 `AverageTrueRange`，周期为 `AtrPeriod`。
   - 扩展 `AverageTrueRange`，周期为 `AtrPeriod + StopDPeriod`。
   - `StochasticOscillator`，周期为 `StochasticPeriod`，%K 只做一次平滑（与 MT5 设置一致）。
   - 三条 `JurikMovingAverage`（高/低/收），均使用 `JmaLength` 和 `JmaPhase` 参数。
2. 每根收盘K线计算：
   - `range = ATR / 2.3`（对应原始代码中的常量 `d`）。
   - `range1 = ATR_extended * 1.5`（对应常量 `s`）。
   - `val3 = |JMA_close - JMA_close[向前两根]|`，复现 MT5 缓冲区差值。
3. 当 `val3 > range` 且 Stochastic 脱离中性区间时：
   - 若 `%K < 47`，进入看空模式（`_trendState = -1`），将止损初始化为 `JMA_high + range1 / 4`，并生成 **sell** 信号。
   - 若 `%K > 53`，进入看多模式（`_trendState = 1`），止损为 `JMA_low - range1 / 4`，并生成 **buy** 信号。
4. 在模式保持不变时，BrainTrend 止损线按 `range1` 向价格靠拢（看空时为 `JMA_high + range1`，看多时为 `JMA_low - range1`）。
5. 信号会在 `SignalBar` 根已完成K线之后触发：
   - Buy 信号会在允许时先平掉空头（`SellClose`），并在允许开仓 (`BuyOpen`) 时建立多头。
   - Sell 信号会在允许时平掉多头（`BuyClose`），并在允许开仓 (`SellOpen`) 时建立空头。

图表会自动叠加 Jurik 平滑的收盘价、Stochastic 振荡器以及交易标记。

## 参数

| 参数 | 说明 | 默认值 |
|------|------|--------|
| `CandleType` | 策略处理的K线类型。 | H4（4小时K线） |
| `AtrPeriod` | BrainTrend 触发所用的主 ATR 周期。 | 7 |
| `StochasticPeriod` | Stochastic %K/%D 周期（%K 仅平滑 1 根）。 | 9 |
| `StopDPeriod` | 附加到第二条 ATR 周期的额外长度（`AtrPeriod + StopDPeriod`）。 | 3 |
| `JmaLength` | 应用于高/低/收的 Jurik MA 周期。 | 7 |
| `JmaPhase` | 传递给 Jurik MA 的相位参数（限制在 [-100, 100]）。 | 100 |
| `SignalBar` | 信号执行前需要等待的已完成K线数量。 | 1 |
| `BuyOpen` / `SellOpen` | 是否允许在信号后建立多头/空头。 | `true` |
| `BuyClose` / `SellClose` | 是否允许在反向信号出现时平多/平空。 | `true` |

请通过策略的 `Volume` 属性或券商设置控制下单手数。

## 与 MT5 原版的差异

- 原策略的资金管理模块（`MM`、`MMMode`、`Deviation_` 等）被标准的 `Volume` 下单方式所取代，没有复刻滑点控制。
- 固定点差的止损/止盈（`StopLoss_`、`TakeProfit_`）未实现。如需保护，请在宿主平台中自行配置风险管理。
- BrainTrend 止损线仅用于内部判定信号，不会被下成挂单。
- Jurik 移动平均来自 StockSharp 的实现，并通过反射应用相位参数，与仓库中其他 BrainTrading 策略保持一致。

## 使用步骤

1. 将策略附加到标的证券，并设置 `CandleType`（建议使用 4 小时K线以贴近原版）。
2. 调整指标参数（`AtrPeriod`、`StochasticPeriod`、`StopDPeriod`、`JmaLength`、`JmaPhase`）以匹配期望的 BrainTrend 灵敏度。
3. 通过 `SignalBar` 控制信号与建仓之间的延迟。
4. 根据交易方向设置 `Volume` 以及开仓/平仓开关（`BuyOpen`、`SellOpen`、`BuyClose`、`SellClose`）。
5. 如有需要，在宿主环境中添加额外的风险控制（止损、仓位限制等）。

启动后，策略会追踪 BrainTrend 反转，在允许的情况下平掉对冲仓位，并在指定延迟后反转净头寸。
