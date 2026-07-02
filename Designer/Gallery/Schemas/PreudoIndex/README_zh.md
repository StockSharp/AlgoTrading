# PseudoIndex 策略说明
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 策略概述

"PseudoIndex"策略旨在利用在 Binance 交易所交易的以太坊和比特币两大主流加密货币的价格比率，创建一个合成指数。该策略通过基于这两种加密货币价格走势实时计算指数，监测它们的相对表现。

![schema](schema.png)

## 策略详情

### 组件

- **数据源**：使用 Binance 上 ETHUSDT 和 BTCUSDT 的[实时价格](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)数据。
- **价格计算**：
  - 追踪 ETHUSDT 和 BTCUSDT 的[收盘价](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)。
  - 计算两者的价格比率，形成合成指数，代表以太坊相对于比特币的相对表现。

### 指数计算

- **蜡烛图形成**：ETH 和 BTC 均采用[5分钟时间框架](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)捕捉短期价格走势。
- **比率计算**：指数计算为 ETH 价格[除以](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/formula.html) BTC 价格，衡量以太坊相对于比特币的价值趋势。

### 可视化

- **图表显示**：计算所得指数绘制在[图表](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)上进行视觉分析，帮助识别趋势并基于指数走势发现潜在交易信号。

## 实现细节

- **平台**：在 StockSharp 平台内实现，利用其先进的实时数据获取和处理功能。
- **技术指标**：该策略仅依赖基础价格信息，不使用其他技术指标，专注于价格比率进行决策。

## 结论

"PseudoIndex"策略通过比较两大主流加密货币的表现，为交易者提供了一种新颖的交易思路，使其能够评估市场情绪，并根据以太坊和比特币的相对强弱做出明智决策。这对于希望基于上述洞察对加密货币持仓进行对冲或多元化配置的交易者尤为实用。
