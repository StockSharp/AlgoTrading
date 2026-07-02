# Bollinger Bands 策略说明
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 策略概述

"Bollinger Bands"策略专为 [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 设计，专注于利用 Bollinger Bands 来把握波动性规律。该策略通过检测价格穿越通道线的时机来确定市场的进场和出场点。

![schema](schema.png)

## 策略详情

### 组件

1. **K线形成**：使用五分钟时间框架生成[K线](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)，并在每根K线收盘时触发分析。
2. **Bollinger Bands 指标**：使用32周期和2.0倍标准差乘数计算 [Bollinger Bands](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html) 的上下轨。
3. **交易信号**：
   - **买入信号**：当K线的[最低价](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)[向下穿越](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) Bollinger Bands 下轨时产生买入信号，表明超卖状态。
   - **卖出信号**：当K线的[最高价](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/converters/converter.html)[向上突破](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/crossing.html) Bollinger Bands 上轨时触发卖出信号，表明超买状态。

### 交易执行

- **订单类型**：进场和出场均使用[市价订单](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)，以确保快速执行。
- **仓位管理**：根据穿越信号开仓，并在反向穿越或满足预定止损/止盈条件时平仓。

### 风险管理

- **止损和止盈**：可配置的设置允许设定固定或百分比形式的[止损和止盈](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)水平，以有效管理风险。
- **资金管理**：策略包含根据可用账户余额和风险水平调整交易规模的参数。

## 结论

"Bollinger Bands"策略为基于波动性和市场状况的交易提供了系统性方法，适合寻求在 StockSharp 平台内建立稳健自动化交易系统的交易者。它将技术指标与精确的交易执行规则相结合，在不同市场环境下提升交易表现。
