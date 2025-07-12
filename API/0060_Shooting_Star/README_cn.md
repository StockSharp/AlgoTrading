# 射击之星形态 (Shooting Star Pattern)

射击之星在上升后出现, 长上影表示可能反转。

若需要确认, 下一根蜡烛收低再进空, 止损在形态高点。

## 详情

- **入场条件**: Shooting star detected and confirmation if enabled.
- **多空方向**: Short only.
- **出场条件**: Stop-loss or discretionary exit.
- **止损**: Yes.
- **默认值**:
  - `ShadowToBodyRatio` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
  - `StopLossPercent` = 1.0m
  - `ConfirmationRequired` = true
- **过滤器**:
  - 类别: Pattern
  - 方向: Short
  - 指标: Candlestick
  - 止损: Yes
  - 复杂度: Intermediate
  - 时间框架: Intraday
  - 季节性: No
  - 神经网络: No
  - 背离: No
  - 风险等级: Medium
