# Exp Fine Tuning MA Candle 策略

## 概览
- 基于 MetaTrader 5 专家顾问 `Exp_FineTuningMACandle.mq5`，核心信号来自 *Fine Tuning MA Candle* 指标的颜色变化。
- 采用 StockSharp 高层 API：订阅单一蜡烛序列，通过 `BindEx` 获取指标数值，并用 `Strategy` 提供的封装方法发送订单。
- 完整保留原策略的开仓/平仓权限控制，并适配 StockSharp 的异步成交模型以避免竞态问题。

## Fine Tuning MA Candle 指标
- 指标对最近 `Length` 根蜡烛的开高低收进行三阶段加权，生成一根“合成蜡烛”。
  - `Rank1`、`Rank2`、`Rank3` 决定三个阶段的权重曲线，`Shift1`、`Shift2`、`Shift3` 在曲线与均匀分布之间进行插值。
  - 权重分布关于窗口中心对称：前半段向中心加速，后半段离中心减速。
  - 归一化后得到平滑的开盘价、最高价、最低价和收盘价。
- 若平滑后的开收差值小于 `GapPoints`（先按品种的最小价位单位转换），则把当前开盘价替换成上一根合成蜡烛的收盘价，用来抹平跳空。
- 颜色定义：`Open < Close` 时为 **2**（多头），`Open > Close` 时为 **0**（空头），相等时为 **1**。策略只依赖颜色序列。
- `PriceShiftPoints` 允许把整根合成蜡烛沿价格轴上下平移若干个最小价位单位。

## 交易规则
- 仅在蜡烛收盘后处理信号。策略维护颜色序列，并读取距离最新收盘蜡烛 `SignalBar` 根的位置。
- **颜色切换为 2（多头）时：**
  - 若允许 `SellPosClose`，先平掉已有的空头仓位。
  - 仓位归零后，在 `BuyPosOpen` 允许的前提下，以 `Volume` 份额市价做多。若需要先平空，做多指令会被缓存，等 `OnPositionChanged` 确认持仓归零后立即下单。
- **颜色切换为 0（空头）时：**
  - 若允许 `BuyPosClose`，先平掉已有的多头仓位。
  - 仓位归零后，在 `SellPosOpen` 允许的前提下，以 `Volume` 份额市价做空，同样利用挂起队列保证“先平后开”。
- 颜色为 1 时不执行任何操作。
- 策略始终只持有一个方向的仓位，不会叠加加仓，也不会在持仓未清空时直接反向。

## 风险控制
- `StopLossPoints` 与 `TakeProfitPoints` 以价位点数表示。收到 `OnNewMyTrade` 回报后，根据真实成交价自动挂出止损和止盈单。
- 每当仓位归零或准备执行新的反向信号时，都会撤销已有的保护性订单，以保持与原 MQL 函数一致的流程。

## 参数说明
| 参数 | 含义 |
| --- | --- |
| `CandleType` | 计算指标时所使用的蜡烛类型/周期。 |
| `Length` | 指标窗口长度（参与加权的蜡烛数量）。 |
| `Rank1`、`Rank2`、`Rank3` | 三个权重阶段的指数系数。 |
| `Shift1`、`Shift2`、`Shift3` | 三个阶段的平滑系数（0~1，越小越贴近原始曲线）。 |
| `GapPoints` | 平滑开收价差若不超过该值就回补前一收盘，单位为价位点。 |
| `SignalBar` | 相对于最新收盘蜡烛向左偏移的根数，`1` 表示直接使用上一根收盘蜡烛。 |
| `BuyPosOpen` / `SellPosOpen` | 是否允许开多/开空。 |
| `BuyPosClose` / `SellPosClose` | 是否允许在出现反向颜色时平多/平空。 |
| `StopLossPoints` | 开仓后止损距离，单位为价位点，填 `0` 代表不挂止损。 |
| `TakeProfitPoints` | 开仓后止盈距离，单位为价位点，填 `0` 代表不挂止盈。 |
| `PriceShiftPoints` | 合成蜡烛沿价格轴的偏移量，单位为价位点。 |

## 实现细节
- 通过 `BindEx` 获取自定义指标返回的复杂结果对象，一次性获得合成 OHLC 及颜色信息。
- 颜色历史只保留 `SignalBar + 2` 条，既能检测到切换，又避免占用额外内存。
- 反向信号先调用 `ClosePosition()`，在 `OnPositionChanged` 观察到仓位变为 0 后再触发等待中的反向市价单，确保执行顺序与原专家一致。
