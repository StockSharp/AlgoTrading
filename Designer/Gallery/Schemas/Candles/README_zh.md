# 基础数据源与图表方块使用示意图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本示意图简明展示了如何在 Designer 平台中使用"Candles"数据源和"Chart"方块。旨在帮助用户理解获取市场数据并以图表形式展示的基本方法。

![schema](schema.png)

## 概述

该示意图展示了获取特定金融工具的K线数据并在图表上显示所需的基本设置。这是 Designer 新用户或希望从简单数据可视化技术入门的用户的基础示例。

## 示意图组件

- **Candles 数据源**：这是从所选金融工具获取[K线数据](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)的主要节点。用户可以指定工具、数据范围和K线时间框架（如1分钟、5分钟K线）。
- **图表方块**：该节点用于在图形界面上[绘制](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)获取的数据，可显示K线的各种属性，如开盘、最高、最低和收盘价格。

## 功能

- **数据获取**：示意图首先使用 Candles 数据源方块中指定的参数获取K线数据。
- **数据可视化**：获取的数据随后传递至图表方块，在 Designer 环境中将K线绘制在图表上。

## 使用场景

本示意图特别适用于：
- 学习如何在 Designer 中设置数据获取和可视化的新用户。
- 希望快速可视化市场数据进行分析的交易者和分析师。
- 教育目的，展示平台内数据源节点与可视化工具之间的基本交互。

## 实际应用

通过理解和使用这一基础设置，用户可以：
- 快速建立市场数据的可视化表示，用于实时或历史数据分析。
- 通过加入 Designer 中提供的其他分析工具或指标来扩展基础示意图。
- 将图表作为构建更复杂交易策略或数据研究的基础模块。

本示意图是 Designer 平台中更广泛教育资源集的一部分，旨在提升用户在数据处理和可视化方面的能力。
