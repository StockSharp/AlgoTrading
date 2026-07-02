# StockSharp Strategy Designer 中 Three White Soldiers 形态检测示例
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 概述

本示例展示了在 StockSharp Strategy Designer 中实现交易策略的方法，该策略利用"Three White Soldiers"K线形态。该形态通常被解读为看涨反转信号，对于希望从动量转变中获利的交易者而言具有重要意义。JSON 方案中描述的配置涉及检测该形态并在其出现时发起交易。

![schema](schema.png)

## 方案描述

该方案概述了一个复杂的工作流程，旨在检测"Three White Soldiers"[形态](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html)并相应地执行交易。以下是关键组件及其作用：

1. **Security 节点**：指定策略应用的[证券](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)。作为主要数据输入源，提供后续分析所需的市场数据。

2. **TimeFrameCandle 节点**：为指定证券生成[K线数据](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)。该节点至关重要，因为它将传入的市场数据处理为形态检测算法可以分析的可用格式（K线）。

3. **形态检测节点**：专门配置用于通过[指标](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)检测"Three White Soldiers"[形态](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html)。该节点分析K线数据，并在识别到形态时触发动作。

4. **图表面板节点**：可视化交易数据，包括K线形态和策略执行的交易。此[组件](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)有助于监控策略的表现，并了解形态如何影响交易决策。

5. **交易节点（买入、卖出）**：这些[节点](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)配置为在检测到形态时执行交易。操作可能因策略中设置的附加条件（如市场状况或其他技术指标）而有所不同。

## 工作流程

- **Security 节点**将市场数据输入**TimeFrameCandle 节点**，在那里数据被转换为K线。
- 这些K线随后传递至**形态检测节点**，该节点配置为识别"Three White Soldiers"形态。
- 检测到形态后，该节点可触发一个或多个**交易节点**，根据策略设计执行买入或卖出订单。
- **图表面板节点**提供K线和已执行交易的实时可视化，有助于评估策略的有效性并在必要时进行调整。

## 实际应用

该配置对于专注于动量型策略的交易者尤为有用，因为早期识别形态可带来可观收益。"Three White Soldiers"形态是看涨反转的强力指标，使该策略适用于：
- 在动量转变明显且清晰的市场中进行波段交易。
- 在高波动性市场中进行日内交易，早期识别趋势反转可带来盈利交易。

## 结论

来自 StockSharp Strategy Designer 的这个示例展示了在算法交易中对K线形态检测的复杂应用。通过自动化检测"Three White Soldiers"等形态，交易者可以更有效地在市场中定位，充分利用历史价格形态的预测能力。详细的可视化和实时数据处理也有助于根据观察到的市场状况和结果来优化策略。
