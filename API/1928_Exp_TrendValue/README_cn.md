# Exp TrendValue 策略

该策略基于 TrendValue 指标。它使用加权移动平均线和 ATR 构建动态支撑与阻力带，当价格突破相反边界时视为趋势反转。

## 入场与出场
- **做多**：出现新的上升趋势时。
- **做空**：出现新的下降趋势时。
- **平多**：出现下降信号或下行带时。
- **平空**：出现上升信号或上行带时。

## 参数
- `BuyPosOpen` / `SellPosOpen` – 允许开多/开空。
- `BuyPosClose` / `SellPosClose` – 允许平多/平空。
- `StopLossPips` – 止损点数。
- `TakeProfitPips` – 止盈点数。
- `MaPeriod` – 加权移动平均周期。
- `ShiftPercent` – 平均线百分比偏移。
- `AtrPeriod` – ATR 周期。
- `AtrSensitivity` – ATR 乘数。
- `CandleType` – K线周期。

## 说明
策略订阅K线数据，仅在K线完成后更新指标并执行交易，同时内部跟踪止损和止盈水平。
