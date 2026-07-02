# Parabolic SAR 策略说明
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 策略概述

"Parabolic SAR"策略旨在利用 [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 中的抛物线停止反转（Parabolic SAR）指标捕捉趋势反转和延续形态。该策略根据价格相对于 Parabolic SAR 点位的运动提供清晰的入场和出场信号。

![schema](schema.png)

## 策略详情

### 组件

- **蜡烛图形成**：采用5分钟[时间框架](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)分析价格走势，确保有效捕捉短期市场动态。
- **Parabolic SAR 指标**：[配置](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)初始加速因子为0.02，加速步长为0.02，最大加速值为0.2。这些设置使指标能够适应市场波动。

### 交易执行

- **入场信号**：当价格从下方穿越 Parabolic SAR 点位时产生买入信号，表明可能出现上涨趋势（[比较](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)）。
- **出场信号**：当价格跌[破](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/comparison.html) Parabolic SAR 点位时发出卖出信号，暗示可能出现下跌趋势。

### 可视化

- **图表显示**：Parabolic SAR 点位与价格蜡烛图一同绘制在[图表](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/chart.html)上，直观呈现趋势方向和潜在交易信号。

## 实现细节

- **平台**：在 StockSharp 平台上实现，充分利用其实时数据获取、指标计算和交易执行的综合功能。
- **指标应用**：Parabolic SAR 直接应用于价格图表，便于即时目测评估趋势变化和交易机会的有效性。

## 结论

"Parabolic SAR"策略非常适合需要基于趋势反转形态获得精确、自动化交易信号的交易者。它充分利用 Parabolic SAR 的动态特性，提供及时的入场和出场时机，在快速运动的市场中增强盈利潜力。
