# Combo EA4 FSF R Updated 5 策略

## 概述
本策略是对 MetaTrader 专家顾问“Combo_EA4FSFrUpdated5”的 StockSharp 版本。它将移动平均线、RSI、随机指标、抛物线 SAR 以及零滞后 MACD 五个模块组合在一起，只有当所有已启用模块给出同一方向的信号时才会开仓，从而忠实地还原原始 EA 的共识过滤逻辑。策略同样保留了可选的追踪止损、基于信号的自动平仓以及在平仓后立即反向入场的功能。

## 指标体系
- **移动平均线**：三条可配置的均线（MA1、MA2、MA3）并使用 ATR 作为缓冲区以过滤噪声穿越，支持五种组合模式，对应原始参数 `MA_MODE` 的所有选项。
- **RSI**：提供超买超卖、斜率趋势、组合模式以及区域模式四种确认方式。
- **随机指标**：包含 %K、%D 以及减速参数，可选启用高低阈值过滤。
- **抛物线 SAR**：以上一根 K 线的收盘价确认趋势方向。
- **零滞后 MACD**：使用零滞后指数移动平均线复现附带的 `ZeroLag_MACD.mq4` 指标，支持趋势结构、零轴穿越或两者结合三种模式。
- **ATR**：用于计算止损/止盈距离，并为均线穿越逻辑提供缓冲带。

## 交易逻辑
### 入场条件
1. 所有启用模块的指标值均已形成（策略会自动等待足够的历史数据）。
2. 根据各自的模式计算出每个模块的多空方向：
   - **移动平均线**：在 ATR 缓冲区的帮助下确认 MA1/MA2/MA3 的穿越方向。
   - **RSI**：支持阈值、动量、组合以及区域四种判定方式。
   - **随机指标**：检查 %K/%D 的交叉，可选附加高低阈值限制。
   - **抛物线 SAR**：要求上一根 K 线收盘价位于 SAR 之上或之下。
   - **零滞后 MACD**：根据模式进行趋势排列或零轴穿越判断。
3. 当所有启用模块同时给出“买入”信号时，发送市价买单；当所有模块给出“卖出”信号时，发送市价卖单；否则保持观望。

### 出场条件
- **信号平仓**：在 `AutoClose` 启用时，使用专用的退出参数（如 `UseMaClosing`、`UseMacdClosing` 等）重复同样的共识判定。多头持仓在所有启用的退出模块给出做空方向时平仓；空头持仓在所有退出模块给出做多方向时平仓。若 `OpenOppositeAfterClose` 为真，则在平仓成交后立即排队反向开仓。
- **保护性价位**：初始止损和止盈基于当前 ATR (`AtrPeriod`) 与 `AtrMultiplier` 的乘积，并通过合约最小变动价近似原始 EA 的点值缓冲。多头止损使用 `ATR × 倍数 − 缓冲`，止盈使用 `ATR × 倍数 + 缓冲`，空头则镜像处理。
- **追踪止损**：当 `UseTrailingStop` 为真时，每根收盘 K 线都会按 `TrailingStop` 指定的点数更新止损位置。
- **硬性平仓**：一旦价格在 K 线内部触发止损或止盈，立即平仓，并不会触发反向入场。

### 仓位管理
- **固定手数**：`UseStaticVolume` 为真时，始终使用 `StaticVolume` 指定的手数。
- **动态手数**：否则根据账户当前权益与 `RiskPercent` 估算下单手数，若无法获取账户或价格信息则回退到基类 `Volume` 设置。

## 参数
| 分组 | 参数 | 说明 |
|------|------|------|
| Entries | `UseMa` | 启用均线确认。 |
| Entries | `MaMode` | 选择均线组合模式（快/中、中/慢、组合等）。 |
| Indicators | `Ma1Period`, `Ma2Period`, `Ma3Period` | 三条均线的周期。 |
| Indicators | `Ma1BufferPeriod`, `Ma2BufferPeriod` | 作为缓冲带的 ATR 周期。 |
| Indicators | `Ma1Method`, `Ma2Method`, `Ma3Method` | 均线类型（SMA、EMA、SMMA、LWMA）。 |
| Indicators | `Ma1Price`, `Ma2Price`, `Ma3Price` | 每条均线使用的价格类型。 |
| Entries | `UseRsi` | 启用 RSI 确认。 |
| Indicators | `RsiPeriod` | RSI 周期。 |
| Entries | `RsiMode` | RSI 判定模式。 |
| Entries | `RsiBuyLevel`, `RsiSellLevel` | 超卖/超买阈值。 |
| Entries | `RsiBuyZone`, `RsiSellZone` | 区域模式使用的上下限。 |
| Entries | `UseStochastic` | 启用随机指标确认。 |
| Indicators | `StochasticK`, `StochasticD`, `StochasticSlowing` | 随机指标参数。 |
| Entries | `UseStochasticHighLow` | 是否要求突破设定的高低阈值。 |
| Entries | `StochasticHigh`, `StochasticLow` | 随机指标高/低阈值。 |
| Entries | `UseSar` | 启用抛物线 SAR 确认。 |
| Indicators | `SarStep`, `SarMax` | SAR 加速度设置。 |
| Entries | `UseMacd` | 启用零滞后 MACD 确认。 |
| Indicators | `MacdFast`, `MacdSlow`, `MacdSignal` | MACD 参数。 |
| Indicators | `MacdPrice` | MACD 使用的价格类型。 |
| Entries | `MacdMode` | MACD 判定模式。 |
| Risk | `UseTrailingStop`, `TrailingStop` | 追踪止损开关与点数距离。 |
| Risk | `UseStaticVolume`, `StaticVolume`, `RiskPercent` | 手数控制。 |
| Risk | `AtrPeriod`, `AtrMultiplier` | 风险管理用 ATR 设置。 |
| Exits | `AutoClose` | 启用信号平仓。 |
| Exits | `OpenOppositeAfterClose` | 信号平仓后立即反向开仓。 |
| Exits | `UseMaClosing`, `MaModeClosing` | 均线退出设置。 |
| Exits | `UseMacdClosing`, `MacdModeClosing` | MACD 退出设置。 |
| Exits | `UseRsiClosing`, `RsiModeClosing` | RSI 退出设置。 |
| Exits | `UseStochasticClosing` | 随机指标退出开关。 |
| Exits | `UseSarClosing` | SAR 退出开关。 |
| General | `CandleType` | 主图时间周期（默认 5 分钟）。 |

## 注意事项
- 策略仅维护一个净持仓（多、空或空仓），以简化原 EA 的“最大同向单”限制。
- 只有因信号平仓时才会排队反向入场；若因止损或止盈离场则不会立即反手。
- 由于不同经纪商的保证金规则差异较大，动态手数仅提供近似估算，实盘前请确认下单数量。
- 与原 EA 一样，需要为零滞后 MACD 与 ATR 准备足够的历史数据以避免信号延迟。
