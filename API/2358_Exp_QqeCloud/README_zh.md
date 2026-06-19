# Exp QQE Cloud 策略
[English](README.md) | [Русский](README_ru.md)

该策略在平滑 RSI 上应用 QQE 指标，仅在预设的交易时段开始时开仓，
并在出现相反信号或到达结束时间时平仓。

## 细节

- **入场条件**:
  - **做多**：在 `StartHour`:`StartMinute` 时，QQE 趋势转向上。
  - **做空**：在 `StartHour`:`StartMinute` 时，QQE 趋势转向下。
- **出场条件**:
  - 相反的 QQE 趋势信号。
  - 时间超过 `StopHour`:`StopMinute`。
- **指标**:
  - RSI（周期 `RsiPeriod`，平滑 `RsiSmoothing`）
  - QQE 带宽，系数 `QqeFactor`
- **止损**：默认无。
- **默认值**:
  - `CandleType` = 1 分钟
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.236
  - `StartHour` = 0，`StartMinute` = 0
  - `StopHour` = 23，`StopMinute` = 59
- **过滤**:
  - 进出场时间窗口
  - 趋势跟随，单一时间框架
