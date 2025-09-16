# MaRsi Trigger 策略
[English](README.md) | [Русский](README_ru.md)

该策略将快慢指数移动平均线（EMA）和 RSI 结合以识别趋势反转。
当快速 EMA 和 RSI 同时高于慢速指标时，认为市场看涨并开多头仓位。
当两者都低于慢速指标时，开空头仓位。参数可控制是否允许做多、做空及其平仓。

## 详情

- **入场条件**：
  - **多头**：快速 EMA > 慢速 EMA 且快速 RSI > 慢速 RSI，并且上一趋势为看空。
  - **空头**：快速 EMA < 慢速 EMA 且快速 RSI < 慢速 RSI，并且上一趋势为看多。
- **出场条件**：
  - **多头**：趋势转为看空且允许平多。
  - **空头**：趋势转为看多且允许平空。
- **指标**：EMA、RSI。
- **止损**：未包含。
- **时间框架**：默认使用4小时K线。
- **参数**：
  - `FastRsiPeriod` = 3
  - `SlowRsiPeriod` = 13
  - `FastMaPeriod` = 5
  - `SlowMaPeriod` = 10
  - `AllowBuyEntry` = true
  - `AllowSellEntry` = true
  - `AllowLongExit` = true
  - `AllowShortExit` = true
