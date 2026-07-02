# SimpleHighBreak 策略说明
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 策略概述

"SimpleHighBreak"策略旨在利用 [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 中价格突破预设高点的机会。该策略专注于识别价格突破15周期高点的时机，以此作为上涨趋势可能延续的信号。

![schema](schema.png)

## 策略详情

### 组件

- **蜡烛图形成**：采用5分钟时间框架生成[蜡烛图](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)，监测市场重要价格走势。
- **高价指标**：计算过去15个周期的[最高价格](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)，确定突破水平。
- **突破检测**：当当前价格[向上突破](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)近期15周期高点时，策略触发买入订单。

### 交易执行

- **订单类型**：市价[订单](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)。
- **入场**：当价格突破15周期高点时下达买入订单。
- **退出策略**：根据特定条件（如设定的时间框架或反转形态）平仓，由策略动态管理。

### 风险管理

- **仓位规模**：根据预设的风险管理规则和当前市场波动性灵活调整。
- **止损和止盈**：入场后立即设置可配置的[止损和止盈](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)水平，以管理风险并锁定利润。

## 实现细节

- **平台**：在 StockSharp 平台内实现，充分利用其实时数据处理和自动化订单管理的强大功能。
- **指标**：主要使用指定周期数的最高价指标来确定入场点。

## 结论

"SimpleHighBreak"策略提供了一种简单而有效的价格突破交易方法，非常适合在波动市场中寻找机会的交易者。它将技术指标与详细的风险管理相结合，力求在降低风险的同时最大化潜在收益。
