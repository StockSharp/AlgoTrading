# StockSharp Strategy Designer 中的三只乌鸦趋势策略说明
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 策略概述

[Strategy Designer](https://doc.stocksharp.com/topics/designer.html) 中的"3 Black Crows Trend"策略采用特定的看跌反转K线形态来预测股市潜在的下跌走势。该自动化交易方案经过精心设计，用于识别并利用重要的价格形态，旨在从熊市趋势中获益。

![schema](schema.png)

## 策略详情

### 形态检测：3 Black Crows

- **描述**：该模块识别"3 Black Crows"[形态](https://doc.stocksharp.com/topics/api/indicators/list_of_indicators/pattern.html)，该形态预示着上升趋势后可能出现看跌反转。该形态由三根连续的长实体K线组成，每根K线的收盘价均低于其开盘价，且每个交易时段的开盘价出现在前一根K线的实体内。
- **条件**：
  - K线1：Open > Close
  - K线2：Open > Close 且 Open < Previous Open
  - K线3：Open > Close 且 Open < Previous Open

### 交易执行

- **订单类型**：市价[订单](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)
- **入场**：确认"3 Black Crows"形态后，启动卖出订单。
- **退出策略**：
  - **止盈**：设定在入场价格以上3%。
  - **止损**：设定在入场价格以下1%。
- **风险管理**：该策略严格遵守初始[止损和止盈](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)设置，不使用追踪止损。

### 交易条件

- **频率**：在[日线级别](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)运行，在每个交易日结束时处理新的K线形态。
- **市价订单**：通过以当前市场价格[下单](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)确保快速执行。

## 实施详情

- **平台**：在 StockSharp 平台上实施，该平台提供形态检测和自动化交易执行的全面功能。
- **设置**：
  - **日志级别**：可配置，以提供详细的操作信息。
  - **参数显示**：可自定义的显示设置，确保操作透明度。
  - **空值处理**：可配置的空值处理方式，以增强健壮性和可靠性。

## 结论

"3 Black Crows Trend"策略专为专注于识别和利用看跌反转形态的交易者而设计。它将精准的形态识别与严格的交易执行规则相结合，旨在提升熊市场景下的潜在盈利能力。
