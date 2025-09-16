# Blau C-Momentum 策略

## 概览
本策略是 MetaTrader 专家顾问 **Exp_BlauCMomentum** 的 StockSharp 移植版本。策略使用可配置时间框架的K线，并通过 Blau 三重平滑动量在两种模式下寻找信号：

- **Breakdown（突破零线）**：当动量穿越零轴时寻找反向入场。
- **Twist（转折）**：当动量斜率发生变化时捕捉趋势反转。

指标可以基于不同的应用价格计算，并能在开仓后自动设置止损/止盈。

## 工作流程
1. 订阅所选时间框架的K线。
2. 计算 Blau C-Momentum：
   - 原始动量为两种应用价格在 `MomentumLength` 根K线间的差值。
   - 通过三次平滑（由 `SmoothingMethod` 指定）并按点值缩放为 ×100/Point。
3. 保存动量历史，用于 `SignalBar` 指定的回溯位移。
4. 生成交易信号：
   - **Breakdown**：前一根K线在零线上方且信号K线≤0 时做多；前一根K线在零线下方且信号K线≥0 时做空。可选的退出开关会在同一条件下关闭反向仓位。
   - **Twist**：比较更早的两根K线。如果动量加速上行且信号K线确认，则做多；若动量加速下行则做空。
5. 使用 `MoneyManagement` 和 `MarginMode` 控制仓位大小。参数为负数时表示固定手数。策略会设置简单的时间锁，避免在同一根K线内重复入场。

## 主要参数
- `MoneyManagement`：资金管理比例，负值代表固定手数。
- `MarginMode`：资金管理模式（按资金占比或按风险占比计算）。
- `StopLossPoints` / `TakeProfitPoints`：止损/止盈距离（单位为价格步长）。
- `EnableLongEntry` / `EnableShortEntry`：是否允许开多/开空。
- `EnableLongExit` / `EnableShortExit`：是否允许根据指标平仓。
- `EntryMode`：`Breakdown` 或 `Twist` 模式。
- `CandleType`：指标计算使用的时间框架。
- `SmoothingMethod`：平滑方法（Simple、Exponential、Smoothed、LinearWeighted、Jurik、TripleExponential、Adaptive）。
- `MomentumLength`、`FirstSmoothLength`、`SecondSmoothLength`、`ThirdSmoothLength`、`Phase`：动量与平滑参数。
- `PriceForClose` / `PriceForOpen`：动量使用的应用价格。
- `SignalBar`：用于判断信号的K线索引（0 表示当前已完成的K线，1 表示上一根等）。

## 使用说明
1. 在 StockSharp 中配置连接、投资组合与交易品种。
2. 创建 `BlauCMomentumStrategy` 实例，设置 `Security`、`Portfolio` 与参数。
3. 调用 `Start()` 后策略会自动订阅K线、计算指标并执行交易。
4. 若 `StopLossPoints`/`TakeProfitPoints` 大于0，策略会自动启用高阶 API 的保护模块。

## 注意事项
- StockSharp 中无法直接获取 MetaTrader 的余额/可用保证金，因此风险模式以 `StopLossPoints` 和 `Security.StepPrice` 近似计算。
- 原版库中的 Parabolic、VIDYA、JurX 等平滑被映射为最接近的现有指标（如 `TripleExponential` ≈ T3，`Adaptive` ≈ KAMA）。
- `SlippagePoints` 仅为兼容而保留，策略始终使用市价单。

## 风险提示
本策略仅供学习交流，请在历史数据和模拟环境中充分验证后再用于真实交易，并根据自身风险承受能力调整仓位。
