# GreenTrade 策略

## 概述
GreenTrade 是原始 MQL5 专家顾问的移植版本。策略通过结合平滑移动平均线（SMMA）的斜率过滤器和相对强弱指数（RSI）的动量确认来捕捉中期趋势。信号只在所选时间框架的已收盘 K 线中计算，并且允许在达到上限之前分批加仓，同时使用固定的风险参数和分步式追踪止损。

## 交易逻辑
1. **指标准备**
   - 使用 `MaPeriod` 周期在中价 `((High + Low) / 2)` 上计算 SMMA。
   - 使用 `RsiPeriod` 周期在收盘价上计算 RSI。
2. **趋势形态过滤**
   - 根据 `ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3` 参数读取四个历史 SMMA 值。
   - 多头条件：`SMMA(shift0) > SMMA(shift1) > SMMA(shift2) > SMMA(shift3)`。
   - 空头条件：`SMMA(shift0) < SMMA(shift1) < SMMA(shift2) < SMMA(shift3)`。
3. **动量确认**
   - 多头信号要求 RSI 高于 `RsiBuyLevel`，空头信号要求 RSI 低于 `RsiSellLevel`。RSI 值取自 `ShiftBar` 根之前的收盘 K 线，与 MQL5 版本忽略当前形成中的 K 线保持一致。
4. **下单规则**
   - 当信号满足并且尚未超过仓位上限时，策略以 `TradeVolume` 发送市价单。
   - 如果存在反向仓位，将先平掉旧仓再打开新方向。
   - 如果已有同方向仓位，则在不超过 `MaxPositions * TradeVolume` 的前提下继续加仓。

## 风险管理
- **初始止损 / 止盈**：每笔新交易会根据 `StopLossPips` 和 `TakeProfitPips` 计算价格目标。点数通过合约的 `PriceStep` 转换为价格；对于五位报价等较小步长的品种，会额外乘以 10，与原始 EA 相同。
- **追踪止损**：当浮动利润超过 `TrailingStopPips + TrailingStepPips` 时，将止损移动到距当前价格 `TrailingStopPips` 的距离。之后每次止损移动都需要价格再前进 `TrailingStepPips`，复制了原策略的阶梯式追踪逻辑。
- **仓位上限**：`MaxPositions` 用于限制最大仓位块数。若加仓会超过上限，则忽略该信号。

## 参数
| 参数 | 说明 | 默认值 |
|------|------|--------|
| `MaPeriod` | 基于中价的 SMMA 周期。 | 67 |
| `ShiftBar`, `ShiftBar1`, `ShiftBar2`, `ShiftBar3` | 用于趋势形态过滤的历史 SMMA 位移（单位：K 线）。 | 1, 1, 2, 3 |
| `RsiPeriod` | RSI 计算周期。 | 57 |
| `RsiBuyLevel` | 确认多头的 RSI 阈值。 | 60 |
| `RsiSellLevel` | 确认空头的 RSI 阈值。 | 36 |
| `TradeVolume` | 每次开仓或加仓的数量。 | 0.1 |
| `StopLossPips` | 初始止损距离（点），0 表示关闭。 | 300 |
| `TakeProfitPips` | 初始止盈距离（点），0 表示关闭。 | 300 |
| `TrailingStopPips` | 追踪止损激活后的固定距离（点），0 表示关闭。 | 12 |
| `TrailingStepPips` | 每次移动追踪止损所需的额外利润（点）。 | 5 |
| `MaxPositions` | 可同时持有的仓位块数（`TradeVolume` 的倍数）。 | 7 |
| `CandleType` | 指标计算所使用的 K 线类型。 | 1 小时 |

## 说明
- 所有计算仅在收盘 K 线上进行，以减少噪音并忠实于原策略。
- 策略内部跟踪仓位状态，通过市价单执行止损、止盈与追踪离场，即使交易所未挂出保护性委托。
- 转换版本保留了原有的点值换算和追踪步长逻辑，同时利用 StockSharp 的高级 API 完成数据订阅与订单执行。
