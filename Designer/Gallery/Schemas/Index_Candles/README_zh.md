# 多蜡烛序列合成指数创建图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本图表文件展示了一种利用 Designer 平台策略库，从多种金融工具的蜡烛序列创建合成指数的策略。该策略汇聚多只证券的数据，构建统一指数，可用于衡量整体市场情绪或行业表现。

![schema](schema.png)

## 策略概述

该策略将多只证券的价格数据合并为单一[指数](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html)。此过程通常采用归一化或加权技术，确保每只证券按比例贡献于最终指数值。

## 图表组件

- **数据采集节点**：负责获取每只所选证券的[蜡烛数据](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)。
- **归一化节点**：对蜡烛数据进行归一化处理，确保其对[最终指数计算](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/index.html)产生均匀影响，消除不同价格量级的干扰。
- **加权节点**：根据市值或历史波动率等预设标准，为每只证券分配权重。
- **指数计算节点**：汇总归一化和加权后的价格数据，计算最终指数值。

## 入场与出场点

- **入场点**：通常不存在传统意义上的入场点，因为该策略不直接涉及交易决策。
- **输出**：主要输出为实时指数值，反映所含证券的整体运动情况。

## 应用场景

交易者和分析师可利用此图表：
- 通过创建自定义指数，监控特定行业或市场的整体表现；
- 将个别证券与更广泛的市场指数进行比较，识别超额收益或表现落后的品种；
- 将自定义指数用作投资组合业绩的基准。

## 教育价值

本策略图表在教育方面尤具价值，有助于理解：
- 指数计算的原理，以及数据归一化和加权在金融分析中的重要性；
- 如何综合多个来源的数据，创建有意义的金融指标。

用户可将此图表导入 Designer 平台，探索并修改该方法，将其适配于不同的证券组合，或提升指数计算方法的复杂程度。

本文件是 Designer 平台多元策略集合的组成部分，旨在提升用户对金融数据聚合与指数构建的理解。
