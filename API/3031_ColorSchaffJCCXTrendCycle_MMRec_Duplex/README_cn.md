# Color Schaff JCCX Trend Cycle MMRec Duplex 策略

## 概述
- 将 MetaTrader 中的双向专家 "ColorSchaffJCCXTrendCycle_MMRec_Duplex" 迁移到 StockSharp 平台。
- 基于 Jurik 移动平均构建两套 Schaff Trend Cycle 流水线，分别跟踪多头与空头的动量反转。
- 内置精简版 MMRec 资金管理逻辑，在连续亏损时自动减小手数。
- 长短方向可使用不同的时间框架与价格输入，方便做非对称配置。

## 指标结构
1. **JCCX 近似**：价格先经过 Jurik 均线得到去趋势序列，再将该序列及其绝对值分别用 Jurik 均线平滑，得到与原始 JCCX 类似的输出。
2. **MACD 层**：快慢 JCCX 输出之差形成基础动量。
3. **双重随机变换**：滚动最高/最低窗口将动量压缩到 -100..+100 范围内，得到最终的 Schaff Trend Cycle 值。
4. **Phase 调节**：`Phase` 参数映射为 0.05–0.95 的平滑系数，用于模拟 Jurik 指标中的相位控制。

长、短两个模块分别执行上述流程，因此可独立选择蜡烛类型与价格来源。

## 交易规则
### 多头模块
- **开仓**：当长周期 STC 上穿 0（当前值 > 0 且延迟值 ≤ 0）时买入，若存在空头仓位会先平仓。
- **平仓**：长周期 STC 跌破 0 且允许平仓时卖出。
- **止盈止损**：可选的止损与止盈距离（单位为价格步长）在每根完成的蜡烛上通过最高价/最低价检测。

### 空头模块
- **开仓**：短周期 STC 下穿 0（当前值 < 0 且延迟值 ≥ 0）时卖出做空，如有多头仓位则先平仓。
- **平仓**：短周期 STC 回到 0 上方且允许平仓时买入。
- **止盈止损**：空头部分执行对称的止损/止盈检查。

`SignalBar` 表示在评估信号前需要跳过的已完成蜡烛数量，默认 `1` 即复现原策略使用上一根蜡烛的方式。

## 资金管理（MMRec）
- 分别维护多头与空头最近交易结果的队列。
- `TotalTrigger` 限制队列长度，仅保留最新 N 笔结果。
- `LossTrigger` 指定在该窗口中出现多少次亏损后切换到 `SmallVolume`。
- 当亏损次数不足时使用默认的 `NormalVolume`。

## 参数
| 分组 | 参数 | 说明 | 默认值 |
| --- | --- | --- | --- |
| Long | `LongCandleType` | 多头模块使用的蜡烛类型/时间框架。 | 8 小时 |
| Long | `LongFastLength` | 多头 JCCX 的快周期。 | 23 |
| Long | `LongSlowLength` | 多头 JCCX 的慢周期。 | 50 |
| Long | `LongSmoothLength` | 对分子/分母进行 Jurik 平滑的周期。 | 8 |
| Long | `LongPhase` | 映射到平滑系数的 Phase 参数。 | 100 |
| Long | `LongCycle` | 随机变换的窗口长度。 | 10 |
| Long | `LongSignalBar` | 评估信号前的延迟蜡烛数。 | 1 |
| Long | `LongAppliedPrice` | 多头模块使用的价格类型。 | Close |
| Long | `LongAllowOpen` / `LongAllowClose` | 是否允许开多/平多。 | true |
| Long | `LongTotalTrigger` | 多头 MMRec 队列的最大长度。 | 5 |
| Long | `LongLossTrigger` | 切换到小手数所需的亏损次数。 | 3 |
| Long | `LongSmallVolume` / `LongNormalVolume` | 多头的减小/默认手数。 | 0.01 / 0.1 |
| Long | `LongStopLoss` / `LongTakeProfit` | 多头止损/止盈距离（价格步长）。 | 1000 / 2000 |
| Short | 同上（参数名前缀为 `Short`）。 | | |

## 风险提示
- 策略使用 `Security.PriceStep` 来换算点数，请确保标的正确设置价格步长。
- 止盈止损在蜡烛收盘后检查，低时间框架能提供更精细的控制。
- MMRec 通过比较开仓与当前蜡烛收盘价估计盈亏，实盘滑点可能导致实际结果有所偏差。

## 使用建议
- 初始可让长短两侧参数一致，以接近原版 EA，然后再尝试不对称配置。
- 将 `SignalBar` 设为 0 可提高响应速度，增大该值则能过滤噪声。
- `Phase` 与平滑周期需要联合优化，以平衡响应速度与稳定性。
