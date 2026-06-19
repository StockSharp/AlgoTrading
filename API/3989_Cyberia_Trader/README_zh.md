# Cyberia Trader 自适应策略

## 概述
**Cyberia Trader 自适应策略** 是 MetaTrader 平台上经典 "CyberiaTrader" 智能交易系统的 C# 版本。策略在 StockSharp 中重构了原始的概率核心，并提供可选的技术指标过滤器。系统持续分析价格波动，评估潜在的反转概率，然后再由 EMA、MACD、CCI、ADX 或分形过滤器确认信号并执行交易。

## 概率引擎
策略的核心是受 MQL 版本启发的概率计算器。它使用自适应采样周期 (`ValuePeriod`)，按固定间隔回看历史 K 线，并将每根 K 线划分为：

* **卖出概率**：当前 K 线为阳线且前一采样为阴线，暗示可能的反转做空机会。
* **买入概率**：当前 K 线为阴线且前一采样为阳线。
* **未定义概率**：其余所有情况。

对于每一类别，策略都会累积平均振幅、出现次数和成功次数，样本数量由 `ValuePeriod × HistoryMultiplier` 控制。自适应搜索会在 `1` 到 `MaxPeriod`（默认 23）之间遍历采样周期，并保留成功率最高的值。内部统计量包括：

* `BuyPossibility`、`SellPossibility`、`UndefinedPossibility`：当前 K 线对应的概率值。
* `BuyPossibilityMid`、`SellPossibilityMid` 等：用于原始决策树的移动平均。
* `PossibilityQuality`、`PossibilitySuccessQuality`：用于诊断与自适应搜索的质量指标。

当历史数据不足时，策略会等待直到概率引擎返回有效样本。

## 指标过滤器
原始 EA 提供一系列布尔开关来控制附加模块，移植版本保持了相同的设计：

* **EMA 过滤器**：比较 `MaPeriod` EMA 在最近两根 K 线的斜率。
* **MACD 过滤器**：检查 MACD 与信号线的相对位置（`MacdFast`、`MacdSlow`、`MacdSignal`）。
* **CCI 过滤器**：使用 `CciPeriod` 和 ±100 阈值标记超买/超卖区域。
* **ADX 过滤器**：基于 `AdxPeriod` 的 +DI 和 −DI 判断趋势方向。
* **分形过滤器**：利用 `FractalDepth` 窗口检测最近的摆动高/低点，并阻止逆向下单。
* **反向检测器**：当概率尖峰超过 `ReversalIndex` 倍平均值时，翻转当前的方向禁用标志。

所有过滤器都可以通过参数独立启用或关闭，行为与 MQL 外部变量一致。

## 交易流程
1. 订阅参数 `CandleType` 指定的 K 线数据。
2. 在每根完结 K 线更新概率统计，并在开启自适应模式时重新选择最佳采样周期。
3. 应用可选指标过滤器以及 Cyberia 原始决策树，确定是否允许买入/卖出。
4. 当判定为买入或卖出信号时，执行市价单，同时尊重全局的 `BlockBuy` 与 `BlockSell` 开关。
5. 如设置了 `StopLossPoints` 或 `TakeProfitPoints`，启动绝对点数止损/止盈保护。
6. 当决策变为 `Unknown` 且概率质量下降时提前平仓。

## 参数说明
| 参数 | 描述 |
| --- | --- |
| `CandleType` | 用于计算的 K 线类型。 |
| `AutoSelectPeriod` | 是否在 `1..MaxPeriod` 范围内自适应搜索最佳采样周期。 |
| `InitialPeriod` | 在禁用自适应时使用的固定采样周期。 |
| `MaxPeriod` | 自适应搜索允许的最大周期（默认 23）。 |
| `HistoryMultiplier` | 每个周期使用的样本倍数。 |
| `SpreadFilter` | 判定概率“有效”的最小价格变动。 |
| `EnableCyberiaLogic` | 是否启用原始概率决策树。 |
| `EnableMa` / `EnableMacd` / `EnableCci` / `EnableAdx` / `EnableFractals` / `EnableReversalDetector` | 启用对应过滤器。 |
| `MaPeriod` | EMA 过滤器长度。 |
| `MacdFast`、`MacdSlow`、`MacdSignal` | MACD 参数设置。 |
| `CciPeriod` | CCI 指标周期。 |
| `AdxPeriod` | ADX 指标周期。 |
| `FractalDepth` | 分形检测使用的窗口长度（建议为奇数且不小于 5）。 |
| `ReversalIndex` | 反向检测器的倍数阈值。 |
| `BlockBuy`、`BlockSell` | 强制禁止开仓的方向。 |
| `TakeProfitPoints`、`StopLossPoints` | 绝对止盈与止损距离。 |

## 注意事项
* 自适应搜索需要足够的历史数据：至少 `ValuePeriod × HistoryMultiplier + ValuePeriod` 根 K 线。
* 代码全部使用 StockSharp 的高阶订阅与指示器绑定 API，并将注释翻译为英文。
* 概率统计保存在策略内部字段，可通过日志或自定义扩展获取详细诊断信息。
