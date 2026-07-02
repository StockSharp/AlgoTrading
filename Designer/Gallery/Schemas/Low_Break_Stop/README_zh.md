# StockSharp Strategy Designer 中的低点突破止损策略示例
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 概述

本示例展示了在 StockSharp Strategy Designer 中配置的"低点突破止损"交易策略。该策略旨在根据特定的低价突破条件执行交易，并结合止损参数进行风险管理。策略利用实时市场数据，识别证券价格在特定周期内跌破预设低点的时机，并以设定的止损条件发起交易。

![schema](schema.png)

## 图表描述

JSON 文件中的图表描述了一套基于价格行为与历史低点关系进行交易的详细工作流程：

1. **证券节点**：主要输入节点，用于[定义目标证券](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)，作为市场价格数据输入的基础。

2. **TimeFrameCandle 节点**：处理传入的市场数据，生成[蜡烛图](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)，这对于分析特定时间段内的价格走势至关重要。

3. **最低值指标节点**：这些节点[计算给定周期内的最低价格](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)，确定发起交易的潜在突破位。

4. **比较节点**：用于[比较](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)当前价格与历史低点，当当前价格跌破设定阈值时触发交易信号，表明出现看跌突破。

5. **图表面板节点**：可视化交易数据和指标，提供策略运行的[图形展示](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)，对于实时监控和策略调整至关重要。

6. **交易执行节点（买入/卖出）**：根据策略逻辑负责[执行交易](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)。本例中，可执行卖出订单以把握预期的价格下跌行情。

7. **止损订单节点**：实施[止损](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)条件以有效管理风险，确保在达到预设亏损阈值时平仓，防范重大不利走势。

## 工作流程

- **证券节点**为策略提供必要的市场数据。
- 数据流入 **TimeFrameCandle 节点**，转换为可用的蜡烛图格式。
- **最低值指标节点**分析这些蜡烛图，确定历史低点。
- **比较节点**监测当前市场价格与这些低点的关系，当价格跌破历史低点时触发交易。
- **交易执行节点**利用这些信号执行卖出订单，预判下跌趋势的持续。
- 与此同时，**止损订单节点**根据预设标准设置止损订单，管理潜在亏损。
- **图表面板节点**显示所有交易和价格走势，直观反映策略的运行情况。

## 实际应用

该配置对于专注于突破策略的交易者尤为有用，识别并把握重大价格走势可带来盈利机会。该策略适用于：
- 高波动市场，价格波动可提供大量交易机会；
- 日内交易者，需要利用快速价格变动并有效控制风险。

## 结论

StockSharp Strategy Designer 中的"低点突破止损"策略示例展示了一种先进的算法交易方法，将实时数据处理与精密的风险管理技术相结合。该策略为在严格遵守风险参数的前提下利用价格突破提供了动态框架，是追求精准、可控交易方法以最大化收益的交易者的重要工具。
