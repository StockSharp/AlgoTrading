# ColorX2MA Digit NN3 MMRec 策略

## 概览
- 复现基于 ColorX2MA Digit 指标的三周期专家策略。
- 内置一个双重平滑均线指标来还原 X2MA 逻辑，可选择 Simple、Exponential、Smoothed、Linear Weighted、Jurik、Kaufman Adaptive 等平滑方法。
- 默认分别在 12 小时、6 小时、3 小时三个周期上运行，每个周期都可以独立控制多头和空头的开仓与平仓。
- 将三个周期的目标仓位相加，与当前仓位对比并通过市价单调整，保证账户净头寸始终等于各周期信号之和。
- `SignalBars` 参数用于要求连续若干根同向信号后才确认方向，对应原始 MQL 程序中的 `SignalBar` 偏移。
- 提供多头/空头开仓与平仓的开关，映射原策略中的 Must Trade 控制项。

## 参数
- **A/B/C Candle Type**：每个周期使用的蜡烛类型（时间框架）。
- **Fast/Slow Method**：X2MA 内部第一条、第二条均线的平滑方法。
- **Fast/Slow Length**：对应均线的周期长度（默认 12 和 5）。
- **Signal Bars**：确认信号需要的连续同向蜡烛数量（默认 1）。
- **Digits**：在计算斜率前对指标值进行的小数位保留，用于模拟原指标的数字化处理。
- **Price Type**：指标取值的价格来源（收盘、开盘、中值、典型价、加权价、简化价、四分位价、TrendFollow、DeMark 等公式）。
- **Allow Long/Short Entry/Exit**：分别允许或禁止某个周期开/平多头或空头。
- **Volume**：当该周期给出信号时希望持有的仓位数量。

## 信号逻辑
1. 仅在蜡烛收盘后处理数据并更新指标。
2. 当双重平滑均线的斜率转为正且持续 `SignalBars` 根蜡烛时，该周期被视为多头：
   - 若允许 `Allow Short Exit`，先平掉现有空单。
   - 若允许 `Allow Long Entry`，按设定的 `Volume` 开多。
3. 当斜率转为负时，该周期转为空头：
   - 若允许 `Allow Long Exit`，先平掉现有多单。
  - 若允许 `Allow Short Entry`，按设定的 `Volume` 开空。
4. 每次目标仓位变化后都会重新计算三周期的总量，并通过市价单调整账户净头寸，使其与目标一致。

## 备注
- JurX、ParMA、T3、VIDYA/AMA 等原库中的特殊平滑方法未加入，如有需要可自行扩展。
- 指标输出会按 `Digits` 进行四舍五入，并且仅对收盘蜡烛计算，避免重绘。
- 策略未内置止损/止盈，原系统的 MMRec 资金管理可通过 `Volume` 参数手动调节仓位实现。
