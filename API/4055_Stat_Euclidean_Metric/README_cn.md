# Stat Euclidean Metric 策略

## 概述
本策略复刻 MetaTrader 专家顾问 `Stat_Euclidean_Metric.mq4` 的核心逻辑。策略基于单一品种、单一周期的 MACD 反转信号进行交易，并可以使用 k 近邻（k-NN）分类器来验证入场信号，分类器会把当前市场结构与存储在二进制文件中的历史特征向量进行比较。

## 交易逻辑
1. 订阅所选蜡烛类型，并在典型价（(最高价 + 最低价 + 收盘价) / 3）上计算 MACD 指标。
2. 当最近三个已完成柱的 MACD 数值满足 `MACD[2] <= MACD[1]` 且 `MACD[1] > MACD[0]` 时，判定为空头反转。
3. 当最近三个已完成柱的 MACD 数值满足 `MACD[2] >= MACD[1]` 且 `MACD[1] < MACD[0]` 时，判定为多头反转。
4. 根据模式不同执行不同操作：
   - **训练模式 (`TrainingMode = true`)**：在出现反转信号时（必要时可先平仓）直接按信号方向开仓，用于复制原 EA 在采集样本时的行为。
   - **分类模式 (`TrainingMode = false`)**：计算五个基于典型价简单移动平均的比率，使用 k-NN 分类器评估胜率，仅当概率跨越阈值时才下单。
5. 通过 `StartProtection` 启动内置风控模块，为订单附加以最小价格步长表示的止盈、止损。

## 分类特征向量
k-NN 模型使用以下在最新收盘柱上计算的比率：
- SMA(89) / SMA(144)
- SMA(144) / SMA(233)
- SMA(21) / SMA(89)
- SMA(55) / SMA(89)
- SMA(2) / SMA(55)

每条样本在数据集中包含 6 个 `double` 值：上述 5 个比率以及标签（`0` 代表亏损，`1` 代表盈利）。在评估时，策略会挑选最近的 `NeighborCount` 个样本，平均它们的标签并把结果视为成功概率。

## 数据集文件
- `BuyDatasetPath`：存放多头交易向量的二进制文件路径。
- `SellDatasetPath`：存放空头交易向量的二进制文件路径。

相对路径会基于 `Environment.CurrentDirectory` 解析。若文件缺失，日志会提示并把数据集视为为空。本实现仅读取数据集，不会自动写入新样本；在训练模式下采集到的新向量需要手动导出。

## 参数说明
- **TrainingMode**：在纯 MACD 交易与分类器辅助交易之间切换。
- **BuyThreshold / SellThreshold**：分类器给出主方向入场的最小概率。
- **AllowInverseEntries**：当概率极低时允许按照反方向入场。
- **InverseBuyThreshold / InverseSellThreshold**：触发反向交易的最大概率。
- **FastLength / SlowLength / SignalLength**：MACD 的 EMA 周期。
- **TakeProfitPoints / StopLossPoints**：按价格步长表示的止盈和止损距离。
- **ClosePositionsOnSignal**：在处理新信号前是否平掉当前净头寸。
- **BuyDatasetPath / SellDatasetPath**：历史特征向量的二进制文件。
- **NeighborCount**：k-NN 投票时使用的邻居数量。
- **CandleType**：所有指标所依赖的蜡烛类型。

## 使用建议
- 在启用分类模式之前，先为数据集参数提供正确的绝对路径或工作目录相对路径。
- 可在训练模式下回测并手动导出样本，用于构建高质量的数据集。
- 通过优化阈值和邻居数量，使分类器适应不同市场或品种。
- 调整策略的 `Volume` 以匹配风险管理，因为策略在需要反向时会下 `Volume + |Position|` 的手数。

## 与 MQL4 版本的差异
- 分类器数据集仅会被读取；原 EA 会在卸载时写入新样本，本策略需要用户自行维护数据文件。
- 止损/止盈由 StockSharp 的 `StartProtection` 统一管理，而不是在下单时直接设置价格。
- 在分类模式下，只要启用了 `ClosePositionsOnSignal`，策略会平掉全部净头寸；原脚本只会在新信号到来时关闭盈利订单。
