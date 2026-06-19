# FT Bill Williams AO 策略

## 概述
**FT Bill Williams AO 策略** 是 MetaTrader 4 智能交易系统 `FT_BillWillams_AO` 的 StockSharp 高阶 API 版本。原始程序
由 FORTRADER.RU 发布，结合了 Bill Williams 的分形、Alligator 指标以及 Awesome Oscillator，用于捕捉趋势突破的
早期信号。移植版本保留原有逻辑，同时采用净头寸模型，反向信号会先平掉当前仓位再开立新仓。

策略在每根收盘蜡烛上执行以下流程：
1. 使用指定的奇数根蜡烛检测看涨与看跌分形。
2. 检查分形价格是否位于 Alligator 牙线之外。
3. 按照 `SignalShift` 参数读取 Awesome Oscillator 的三个历史值，确认加速条件 (`A>B`、`B<C` 以及正负号)。
4. 根据上/下突破公式 `High[SignalShift] + IndentPoints * 步长` 或 `Low[SignalShift] - IndentPoints * 步长` 计算触发价。
5. 根据参数选择执行颚线平仓、反向信号平仓以及 Gragus 拖尾规则。

## 入场规则
### 多头
- 检测到看涨分形且其高点高于 Alligator 的牙线。
- AO 在 `SignalShift+2`、`SignalShift+1`、`SignalShift` 三根蜡烛上的值均大于 0，且满足 `A > B`、`B < C`。
- 触发价 = `High[SignalShift] + IndentPoints * PriceStep`。
- 当前完成蜡烛最高价突破触发价且 AO 仍在上升 (`C > B`) 时买入，必要时先平空后开多。

### 空头
- 检测到看跌分形且其低点低于牙线。
- AO 三个历史值均小于 0，且 `A < B`、`B > C`。
- 触发价 = `Low[SignalShift] - IndentPoints * PriceStep`。
- 当前蜡烛最低价跌破触发价且 AO 继续下降 (`C < B`) 时做空或翻仓。

## 出场与风险控制
- `StopLossPoints` 与 `TakeProfitPoints` 以 MetaTrader 点数表示，系统根据品种价格步长换算为实际价差。
- `CloseDropTeeth` 控制是否在价格穿越 Alligator 颚线时立即离场，可选择当前收盘或上一根收盘价。
- `CloseReverseSignal` 可设置为在出现相反分形或当相反方向的突破信号准备就绪时平仓。
- 启用 `UseTrailing` 时执行 Gragus 拖尾：若嘴唇线斜率大于辅助 SMA，则止损跟随嘴唇；否则跟随牙线，两种情况
  均要求价格距离目标线至少 12 点。

## 参数说明
| 参数 | 说明 |
| --- | --- |
| `TradeVolume` | 每次交易的手数，同时赋值给 `Strategy.Volume`。 |
| `CandleType` | 使用的蜡烛数据类型及时间框。 |
| `FractalPeriod` | 分形窗口大小，应为奇数（默认 5）。 |
| `IndentPoints` | 突破触发价的额外点数偏移。 |
| `JawPeriod` / `TeethPeriod` / `LipsPeriod` | Alligator 下颚、牙齿、嘴唇的平滑均线长度。 |
| `JawShift` / `TeethShift` / `LipsShift` | 对应均线的前移位数（单位：蜡烛）。 |
| `CloseDropTeeth` | 颚线平仓模式：关闭、当前收盘穿越、上一收盘穿越。 |
| `CloseReverseSignal` | 反向信号离场设置：关闭、出现相反分形、相反突破信号就绪。 |
| `UseTrailing` | 是否启用 Gragus 拖尾逻辑。 |
| `TrendSmaPeriod` | 拖尾时使用的辅助 SMA 周期。 |
| `StopLossPoints` | 初始止损点数，设为 0 可禁用。 |
| `TakeProfitPoints` | 初始止盈点数，设为 0 可禁用。 |
| `SignalShift` | 读取 AO 与高/低价时回看的已完成蜡烛数量。 |

## 其他注意事项
- 策略优先使用 `PriceStep`，若不可用则退回 `MinPriceStep`，最后使用默认步长 `0.0001`。
- 采用净头寸模型，同方向不会累加订单；翻向时会先平仓再开新仓。
- 为保持原版表现，请使用奇数的 `FractalPeriod`（默认 5）。
- `IndentPoints`、`StopLossPoints`、`TakeProfitPoints` 都以 MetaTrader 点数计量，需结合品种精度调整。
