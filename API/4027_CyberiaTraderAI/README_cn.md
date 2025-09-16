# Cyberia Trader AI 策略

该策略是 **CyberiaTrader.mq4 (build 8553)** 智能交易系统的 StockSharp 版本。原始 MQL 程序由一个概率模型和多种趋势过滤器组成。本转换保持同样的结构：概率模型会在不同的采样周期之间搜索最佳结果，随后 MACD、EMA 与反转检测器可以阻止或允许交易。

## 指标与内部模型

- **概率引擎**：遍历 1 到 `MaxPeriod` 的候选采样周期，并对每个周期处理 `SamplesPerPeriod` 个历史区间。模型会计算：
  - 根据采样周期间隔的多根 1 分钟 K 线确定买/卖/观望的决策方向。
  - 统计买、卖与模糊方向的平均“可能性”振幅以及超过 `SpreadThreshold` 的成功次数。
  - 成功率最高的周期被选为当前交易配置。
- **EMA 趋势过滤器**：启用 `EnableMa` 后使用指数移动平均线阻止逆势交易。
- **MACD 过滤器**：启用 `EnableMacd` 后使用 MACD 的主线与信号线过滤动量方向。
- **反转检测器**：启用 `EnableReversalDetector` 后，当概率值超过平均值的 `ReversalFactor` 倍时翻转允许的方向。

## 参数说明

| 名称 | 说明 |
| --- | --- |
| `MaxPeriod` | 概率模型搜索的最大采样步长。 |
| `SamplesPerPeriod` | 每个候选周期评估的历史区间数量（对应 MQL 的 `ValuesPeriodCount`）。 |
| `SpreadThreshold` | 判定概率事件为“成功”的最小振幅。 |
| `EnableCyberiaLogic` | 是否启用 Cyberia 概率开关以屏蔽买入或卖出。 |
| `EnableMacd` | 是否启用 MACD 动量过滤器。 |
| `EnableMa` | 是否启用 EMA 趋势过滤器。 |
| `EnableReversalDetector` | 是否启用概率尖峰反转检测器。 |
| `MaPeriod` | EMA 滤波器的周期长度。 |
| `MacdFast` / `MacdSlow` / `MacdSignal` | MACD 的快速 EMA、慢速 EMA 与信号线周期。 |
| `ReversalFactor` | 触发反转检测器的倍数阈值。 |
| `CandleType` | 策略处理的 K 线类型（默认为 1 分钟）。 |
| `TakeProfitPercent` | 以百分比表示的可选止盈距离。 |
| `StopLossPercent` | 以百分比表示的可选止损距离。 |

## 交易逻辑

1. 每根完成的 K 线都会更新本地历史缓存，并对 1 至 `MaxPeriod` 的所有周期重新计算概率统计。成功率最高的周期将成为当前有效配置。
2. Cyberia 逻辑根据原脚本的规则设置 `DisableBuy` 与 `DisableSell`：
   - 当采样周期增大或减小时比较买/卖平均可能性及其成功加权版本。
   - 若最新可能性大于成功平均值的两倍，则禁止新的信号。
3. 可选过滤器按顺序应用：先 MACD，再 EMA，最后是反转检测器。
4. 当没有持仓时，若当前决策为买入（或卖出）、对应可能性超过成功平均值且反方向被禁用，则开仓。
5. 当存在持仓时，如果概率模型反向或过滤器禁止当前方向，则立即平仓。
6. 在设置了非零风险参数时，`StartProtection` 会启动与原策略类似的资金保护机制。

## 转换说明

- 保留了原始概率计算，但将基于点差的判断替换为可配置的 `SpreadThreshold`。
- MQL 中的自动手数与账户状态打印未移植，StockSharp 中通过 `Volume` 控制下单数量。
- MoneyTrain 与 Pipsator 模块被简化为统一的进出场逻辑，以便使用高级 API。
- 策略在图表上绘制 K 线、EMA 与 MACD，便于在 Designer 中进行验证。
