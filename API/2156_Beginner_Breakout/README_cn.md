# Beginner Breakout 策略

利用最近 `Period` 根K线的最高价和最低价构建通道。当收盘价接近上边界时做多，接近下边界时做空。

## 入场规则
- **做多**：Close >= high − (high − low) * `ShiftPercent` / 100 且当前趋势不是向上。
- **做空**：Close <= low + (high − low) * `ShiftPercent` / 100 且当前趋势不是向下。

## 出场规则
- 相反信号会平掉当前仓位并在相反方向开仓。

## 参数
- `Period` – 计算通道时回溯的K线数量。
- `ShiftPercent` – 相对通道边界的百分比偏移。
- `CandleType` – 使用的K线周期。
- `Volume` – 交易量。
- `StopLoss` – 以价格单位表示的止损。
- `TakeProfit` – 以价格单位表示的止盈。

## 指标
- Highest
- Lowest
