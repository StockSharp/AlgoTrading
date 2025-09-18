# Macd Pattern Trader

## 概述
Macd Pattern Trader 将 MetaTrader 4 顾问 `MacdPatternTraderv05cb` 移植到 StockSharp 的高级策略 API。策略完全依靠 MACD 直方图形态：当零轴下方出现双顶时做空，零轴上方出现镜像双底时做多。风控逻辑保持原样——每次入场均为市价单，并使用以最小价位表示的固定止损与止盈。

## 策略逻辑
### 数据流
* 仅订阅一种蜡烛序列（默认 15 分钟）。每根收盘蜡烛都会送入 `MovingAverageConvergenceDivergence` 指标，参数与原始 MT4 版本一致 `(fast = 13, slow = 5, signal = 1)`。
* 只使用 MACD 主线。策略缓存最近三根已完成蜡烛的数值，以模拟 MetaTrader 中 `iMACD(..., MODE_MAIN, shift=1..3)` 的调用方式。

### 做多条件
1. **武装** —— MACD 必须上穿 `Bullish Trigger`（默认 `0.0015`）。一旦跌回零轴下方，所有多头状态立即清空。
2. **回撤窗口** —— 完成武装后，MACD 需要回落到 `Bullish Reset` 以下（默认 `0.0005`），此区间用于等待多头反弹结构。
3. **形态确认** —— 在窗口有效期内，缓存的三个数值必须满足：
   * `macd_curr > macd_last`，动量重新向上；
   * `macd_last < macd_last3`，上一根蜡烛创出局部低点；
   * `macd_curr > Bullish Reset` 且 `macd_last < Bullish Reset`，确认从浅幅回撤区反弹。
4. **执行** —— 触发后立即以市价买入。如账户当前持有空单，委托量会自动加上需要回补的数量，确保最终持仓为目标多头规模。

### 做空条件
1. **武装** —— MACD 必须跌破 `-Bearish Trigger`（默认 `-0.0015`）。一旦重新上穿零轴，所有空头状态被清除。
2. **回撤窗口** —— 武装后 MACD 需要反弹到 `-Bearish Reset` 以上（默认 `-0.0005`）。
3. **形态确认** —— 在窗口有效期内需要满足：
   * `macd_curr < macd_last`；
   * `macd_last > macd_last3`；
   * `macd_curr < -Bearish Reset` 且 `macd_last > -Bearish Reset`。
4. **执行** —— 立即以市价卖出。如持有多单，委托量会包含平仓数量，从而在成交后得到目标净空头。

### 风险控制
* **固定止损 / 止盈** —— 参数以点（最小价位）表示，运行时会乘以品种的 `PriceStep` 并通过 `StartProtection` 下达保护单。设置为 `0` 可关闭对应防线。
* **每个窗口仅触发一次** —— 成交后会清除武装与窗口标志，避免同一 MACD 形态反复触发。

## 参数
* **Trade Volume** —— 市价委托量。若需要反向建仓，会同时平掉旧仓位。
* **Fast EMA / Slow EMA / Signal EMA** —— MACD 参数，默认值复刻原策略，可用于优化。
* **Bullish Trigger / Reset** —— 多头武装阈值及回撤区间（指标单位）。
* **Bearish Trigger / Reset** —— 空头对应的阈值，触发条件在运行时会自动取负号。
* **Stop Loss / Take Profit** —— 以点表示的止损与止盈距离，`0` 表示禁用。
* **Candle Type** —— 指标与信号所使用的蜡烛周期。

## 实现细节
* 完全依赖 StockSharp 高级 API：`SubscribeCandles` 提供数据流，`StartProtection` 复现 MT4 的止损/止盈行为。
* MACD 缓存确保决策基于最近三根完整蜡烛，与 MetaTrader 的 `shift=1..3` 调用保持一致。
* 当前包内仅提供 C# 版本，暂无 Python 实现。
