# StockSharp Strategy Designer 中的市场深度处理示例
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 概述

本示例展示了 StockSharp Strategy Designer 中处理市场深度数据的配置方案。市场深度数据（通常称为"订单簿"）包含证券在不同价格水平上的买卖订单信息，对于需要实时分析各价格水平供需动态的策略至关重要。

![schema](schema.png)

## 图表描述

该图表由多个相互关联的组件构成，用于获取、处理和展示市场深度信息：

1. **证券节点**：该节点代表将要获取市场深度的[证券](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/variable.html)（如股票、期货或其他金融工具），是定义所分析市场或工具的基本元素。

2. **TimeFrameCandle 节点**：处理按指定时间框架（示例中为5分钟）汇总的证券[蜡烛数据](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)，可用于将市场深度变化与价格走势进行关联分析。

3. **市场深度节点**：用于捕获并响应[市场深度](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book.html)的实时变化，包含处理传入市场深度数据的设置，提供当前买卖订单的实时信息。

4. **图表面板节点**：将蜡烛图数据可视化展示在[图表](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)上，帮助交易者或算法更好地理解市场状况并做出明智决策。

5. **市场深度面板节点**：专门用于在[专用面板](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/market_depths/order_book_panel.html)中展示市场深度数据，可提供最优买卖价格高亮显示、市场深度可视化等功能。

## 工作流程

- **证券节点**输出的数据分别作为 **TimeFrameCandle 节点**和**市场深度节点**的输入。
- **TimeFrameCandle 节点**处理数据，为指定时间框架生成蜡烛图，可用于趋势分析或其他技术分析。
- **市场深度节点**实时处理指定证券的市场深度，可根据特定条件（如某价格水平上买卖订单的重大失衡）触发交易决策。
- 通过**图表面板节点**和**市场深度面板节点**进行可视化，确保数据不仅用于交易逻辑，也便于人工查阅和监控。

## 实际应用

该配置可应用于多种交易策略，包括：
- **高频交易（HFT）**：订单簿的细微变化可能预示着潜在的盈利交易机会。
- **套利策略**：比较多个交易所的订单簿，利用价格差异获利。
- **做市策略**：深入理解订单簿两端的信息，对于合理设置买卖报价至关重要。

## 结论

JSON 文件中的图表展示了在 StockSharp Strategy Designer 中处理市场深度数据的全面方法。通过将实时数据处理与先进的可视化工具相结合，该配置帮助交易者和算法根据订单簿状态快速做出数据驱动的决策。本示例为开发需要深入洞察市场动态的复杂交易策略提供了坚实的基础。
