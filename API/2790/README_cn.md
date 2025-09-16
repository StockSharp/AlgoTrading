# Alexav SpeedUp M1 策略

## 概述
- 将 MetaTrader 5 上的 “Alexav SpeedUp M1” 专家顾问移植到 StockSharp 高级 API。
- 默认使用 1 分钟周期，在市场出现异常放大的 K 线实体时触发交易。
- 只保持一个净头寸：当检测到强势蜡烛时按照其方向建仓，并结合固定止损、固定止盈与阶梯式移动止损进行风险管理。
- 所有参数以点（pip）为单位输入，策略会根据品种的最小跳动和小数位数自动换算成价格距离。

## 原版思路与移植差异
- 原始 EA 依赖对冲账户，会同时开多单和空单。StockSharp 策略在净持仓模式下运行，因此本移植版每次只保留一个方向的仓位，按照大实体蜡烛的方向入场。
- 移动止损沿用 MT5 逻辑：只有当利润达到 `TrailingStop + TrailingStep` 点时才会把止损向盈利方向推进一个 `TrailingStop` 距离，并且只有当价格再前进一个 `TrailingStep` 才会继续上调。
- Pip 距离通过乘以最小跳动值转换为价格单位；对于 3 位或 5 位小数的外汇品种，会额外乘以 10 以模拟 MT5 对 pip 的处理。

## 入场规则
1. 使用所选周期的已完成 K 线（默认 1 分钟）。
2. 计算 K 线实体：`abs(Close - Open)`。
3. 当实体大于 `MinimumBodySizePips * pipSize` 且当前没有持仓时，按照蜡烛方向开仓：
   - 阳线 → 建多单。
   - 阴线 → 建空单。

## 离场规则
- **止损**：距离开仓价 `StopLossPips * pipSize`，若参数为 0 则不设置。
- **止盈**：距离开仓价 `TakeProfitPips * pipSize`，若参数为 0 则不设置。
- **移动止损**（`TrailingStopPips > 0` 且 `TrailingStepPips > 0` 时启用）：
  - 当浮盈达到 `TrailingStopPips + TrailingStepPips` 点后激活。
  - 多头：止损更新为 `Close - TrailingStopPips * pipSize`，且只有当价格较上一次止损至少再前进一个 `TrailingStep` 才会继续上调。
  - 空头：止损更新为 `Close + TrailingStopPips * pipSize`，并满足同样的阶梯条件。

## 参数说明
- `OrderVolume` – 交易手数，默认 `0.1`。
- `StopLossPips` – 止损距离（点），默认 `30`。
- `TakeProfitPips` – 止盈距离（点），默认 `90`。
- `TrailingStopPips` – 移动止损距离（点），默认 `10`。
- `TrailingStepPips` – 移动止损每次调整所需的最小利润（点），默认 `5`，启用移动止损时必须大于 0。
- `MinimumBodySizePips` – 触发信号所需的最小实体（点），默认 `100`。
- `CandleType` – 计算所用的蜡烛周期，默认 `1 Minute`。

## 可视化
- 策略在有可用图表区域时会自动绘制所选蜡烛序列以及自身成交，便于回测与调试。

## 使用建议
- 默认参数复制自 MT5 版本，可根据标的波动率调整各个距离参数。
- 由于只支持净持仓，请勿在需要同时持有多空仓位的对冲环境中运行。
- 若标的最小跳动较大，请相应减小 pip 参数，以保持相似的价格距离。
