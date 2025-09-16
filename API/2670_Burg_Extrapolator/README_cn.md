# Burg Extrapolator 策略

## 概述

Burg Extrapolator 策略在 StockSharp 高级 API 上重建了 MetaTrader 专家顾问 “Burg Extrapolator”。策略通过 Burg 算法估计自回归（AR）模型的系数，用来预测未来的开盘价路径。当预测路径的波动幅度超过设定阈值时，策略会选择开仓或平仓。

## 工作流程

1. **数据准备**
   - 在每根已完成的 K 线上收集 `PastBars` 根开盘价。
   - 可选地将价格转换为对数动量 `log(p[i]/p[i-1])` 或变化率 `p[i]/p[i-1]-1`。
   - 当使用原始价格时，先减去滑动平均值进行中心化处理。
2. **AR 模型**
   - 根据 `ModelOrderFraction` 和 `PastBars` 计算 AR 阶数（向下取整）。
   - 采用 Burg 算法同时最小化前向和后向误差，得到稳定的系数。
   - 使用这些系数外推未来的若干个时间点（预测步长 = `PastBars - order - 1`），并将结果还原成价格序列。
3. **信号判断**
   - 在预测路径上寻找最高值和最低值。
   - 若预测的幅度超过 `MinProfitPips`，生成顺势开仓信号。
   - 若预测的幅度超过 `MaxLossPips`，生成平仓信号，保护已有头寸。
4. **下单执行**
   - 依据风险参数计算下单数量，使用市价单入场。
   - 出现反向信号或触发保护条件时，使用市价单离场。

## 参数说明

- `RiskPercent`：单笔交易占用的账户风险比例（%），用于结合止损计算下单数量。
- `MaxPositions`：同方向允许的最大头寸规模（按下单数量的倍数计算）。
- `MinProfitPips`：预测的最小盈利幅度（点），超过该值才允许开仓。
- `MaxLossPips`：预测的最大亏损幅度（点），超过该值将触发平仓。
- `TakeProfitPips`：固定止盈距离（点），为 0 时禁用。
- `StopLossPips`：固定止损距离（点），风险管理和仓位计算所需。
- `TrailingStopPips`：追踪止损距离（点），仅在启用止损时生效。
- `PastBars`：参与 Burg 模型计算的历史 K 线数量。
- `ModelOrderFraction`：AR 模型阶数占 `PastBars` 的比例。
- `UseMomentum`：是否使用对数动量作为输入。
- `UseRateOfChange`：在未启用动量时，是否改用变化率作为输入。
- `OrderVolume`：无法根据风险计算时使用的备用下单手数。
- `CandleType`：用于计算的 K 线类型/时间框架。

## 交易规则

- **入场**：预测路径的波动幅度大于 `MinProfitPips` 时，如果先出现预测高点则做多，如果先出现预测低点则做空。
- **离场**：当预测幅度大于 `MaxLossPips` 或出现反向信号时平掉当前仓位。
- **保护**：通过 `StartProtection` 配置止损、止盈和追踪止损，内部自动将点值转换为绝对价格。
- **仓位管理**：当 `StopLossPips` 和 `RiskPercent` 均大于 0 时，下单数量 = 风险资金 / 止损距离；否则使用 `OrderVolume`。

## 实现细节

- 仅处理 `CandleStates.Finished` 的已完成 K 线，避免未来函数。
- 不调用指标的 `GetValue`，在 `Bind` 回调内直接处理原始数据。
- 通过 `SubscribeCandles` 订阅数据，并使用 `StartProtection` 管理风险，符合 StockSharp 高级 API 的最佳实践。
- 追踪止损使用框架自带的保护模块，行为与原版 EA 保持一致。

## 使用建议

- 合理设置 `PastBars` 与 `ModelOrderFraction`，确保预测步长保持为正值。
- 动量和变化率模式要求价格为正值；若品种可能出现负价，建议使用原始价格模式。
- 若未配置有效的止损距离，策略将使用备用手数，风险控制能力会下降。
- 建议在回测中验证不同参数组合，以防止 AR 模型在趋势行情中过拟合。

## 文件结构

- `CS/BurgExtrapolatorStrategy.cs`：C# 实现文件。
- `README.md`：英文说明。
- `README_ru.md`：俄文说明。
- `README_cn.md`：本中文说明。

按照任务要求，本策略暂不提供 Python 版本。
