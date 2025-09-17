# Exp AFIRMA 策略

## 概述

**Exp AFIRMA** 策略在 StockSharp 平台上重现了 MetaTrader 顾问 `Exp_AFIRMA.mq5`。核心为 AFIRMA 指标（Adaptive Finite
Impulse Response Moving Average），该指标先用带窗函数的 FIR 滤波器平滑价格，再利用 ARMA 预测未来方向。本移植版完全
保留原始逻辑：当 ARMA 预测线由下转上时开多，由上转下时开空。所有计算基于选定时间框架的已完成 K 线（默认 4 小时）。

策略在每根蜡烛收盘后记录最新的 ARMA 值，并检查三个连续收盘的斜率。当出现反向信号时，先撤销当前方向的持仓，再建立
新的反向仓位。可选的止损/止盈通过 StockSharp 的 `StartProtection` 机制实现。

## 交易逻辑

1. **指标计算**
   - 自定义的 `AfirmaIndicator` 复刻了 AFIRMA 算法。FIR 阶段使用 `Taps` 个系数，按所选窗口函数（矩形、两种 Hanning、
     Blackman、Blackman-Harris）加权，参数 `Periods` 控制频带宽度（与原 MQL 输入一致）。
   - 预测阶段采用与原始代码相同的最小二乘公式（系数 `sx2…sx6`），输出 FIR 与 ARMA 两条曲线供图形显示。
2. **信号判定**
   - 每根 K 线收盘时保存新的 ARMA 值。`SignalBar` 指定跳过的收盘数，默认值 1 表示分析 `t-1、t-2、t-3` 三根已收盘 K
     线并在第 `t` 根开盘执行。
   - **多头条件**：`ARMA[t-2] < ARMA[t-3]` 且 `ARMA[t-1] > ARMA[t-2]`。此时允许平空并根据 `TradeVolume` 建立/加仓多头。
   - **空头条件**：`ARMA[t-2] > ARMA[t-3]` 且 `ARMA[t-1] < ARMA[t-2]`。此时允许平多并建立/加仓空头。
3. **仓位管理**
   - 策略仅维持一个净头寸。触发信号后，仓位目标调整到 `±TradeVolume`。
   - 若启用 `StopLossPoints` 或 `TakeProfitPoints`，在 `OnStarted` 中调用 `StartProtection`，以价格单位设置保护单。
   - 开仓前会取消所有挂单，避免旧订单与新信号冲突。

## 参数

| 参数 | 说明 |
|------|------|
| `TradeVolume` | 基础交易量，决定多空目标仓位。 |
| `CandleType` | 指标计算所用的蜡烛类型/时间框架。 |
| `Periods` | FIR 滤波器的带宽参数（等同于原顾问的 `1/(2*Periods)` 设置）。 |
| `Taps` | FIR 系数数量，内部会自动调整为奇数。 |
| `Window` | FIR 的窗函数：Rectangular、Hanning1、Hanning2、Blackman、BlackmanHarris。 |
| `SignalBar` | 用于确认信号的历史收盘数量；`1` 代表最近已收盘的一根 K 线。 |
| `EnableBuyEntries` / `EnableSellEntries` | 是否允许开多/开空。 |
| `EnableBuyExits` / `EnableSellExits` | 是否允许自动平多/平空。 |
| `StopLossPoints` | 止损距离，使用价格单位。 |
| `TakeProfitPoints` | 止盈距离，使用价格单位。 |

## 转换说明

- 原策略中的资金管理选项（`MM`、`MMMode`、`Deviation_`）未迁移，改为单一的 `TradeVolume`。可通过账户或外部模块实现更
  灵活的仓位控制与滑点处理。
- MQL 版本以“点”为单位发送止损/止盈，这里直接使用价格差值，方便在不同品种之间复用。如需与点数匹配，请自行乘以价格步长。
- 当 `SignalBar = 1` 时，策略读取最近三根**已完成**蜡烛的 ARMA 值，并在下一根蜡烛开盘下单，与原 `CopyBuffer` 调用保持一致。
  将 `SignalBar` 设为 `0` 也可以使用，但由于计算在收盘后进行，因此仍然基于最新收盘价。
- `AfirmaIndicator` 完整实现了 FIR+ARMA 的数学模型，可与 `DrawIndicator` 结合，在图表上同时显示 FIR 与 ARMA 曲线。

## 使用建议

1. 绑定所需的证券与投资组合，设置 `TradeVolume` 并选择合适的 `CandleType`。
2. 根据策略方向需求调整多空开仓和平仓开关。
3. 若需要固定的止损/止盈，设置 `StopLossPoints` 与 `TakeProfitPoints`；留为 0 时策略仅依赖反向信号退出。
4. 在调整 `Periods`、`Taps`、`SignalBar` 等参数时配合图表观察 FIR/ARMA 线的走势以及成交记录，确保行为与预期一致。
