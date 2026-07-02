# 移动平均线策略图
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

本文件包含一个基于移动平均线的交易策略图表，使用 Designer 平台的策略库设计而成。该策略利用移动平均线的交叉来生成买卖信号，这是金融市场中用于判断动量和确认趋势的常用方法。

![schema](schema.png)

## 策略概述

该策略结合两条移动平均线：

- **短期移动平均线**：一条对价格变化响应更快的[移动平均线](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)。
- **长期移动平均线**：一条变化较慢的[移动平均线](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)，能提供更平滑的价格趋势图像。

## 入场与出场规则

- **买入信号**：当短期移动平均线从下方[穿越](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)长期移动平均线时，策略生成[买入](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)信号，表明上涨趋势。
- **卖出信号**：反之，当短期移动平均线从上方[穿越](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html)长期移动平均线时，发出[卖出](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)信号，预示潜在的下跌趋势。

## 图表细节

图表直观展示了策略的逻辑流程：

- **移动平均线计算**：节点根据用户自定义参数（如周期和移动平均线类型，例如简单、指数）计算移动平均线。
- **比较节点**：评估交叉条件，以确定是否进行开仓或平仓操作。
- **交易动作**：根据比较节点的评估结果执行买入或卖出订单的节点。

## 应用场景

交易者可将此图表导入 Designer 平台，用于：
- 使用历史数据对策略进行回测，了解其有效性；
- 修改移动平均线参数或逻辑，以更好地适应特定交易需求或市场条件；
- 经充分测试后在实盘交易环境中部署该策略。

## 教育价值

本策略图表是帮助初学者了解技术分析和策略设计基础的教学工具，同时也为高级用户开发更复杂策略提供了基础框架。

本文件是 Designer 平台提供的综合交易策略集合的一部分，旨在提升用户的交易技能和策略开发能力。
