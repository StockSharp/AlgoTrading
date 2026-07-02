# Bullish8020 策略说明
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## 策略概述

"Bullish8020"策略专为 [StockSharp Designer](https://doc.stocksharp.com/topics/designer.html) 打造，旨在高精度地把握特定看涨K线形态。该策略通过独特的形态分析结合成交量和价格行为，旨在识别看涨情绪强烈的市场机会。

![schema](schema.png)

## 策略详情

### 形态检测：Bullish8020

- **描述**：该策略检测看涨场景，其中[开盘价](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)低于收盘价，且实体大小是两根影线之和的四倍，表明强烈的买入压力。
- **K线形态**：'Bullish8020' 检查条件 `(O < C) && (B >= 4*(BS+TS))`，其中 `O` 为开盘价，`C` 为收盘价，`B` 为实体大小，`BS` 为下影线，`TS` 为上影线。

### 交易执行

- **订单类型**：市价[订单](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)
- **入场**：当"Bullish8020"[形态](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)确认，预示潜在上涨走势时买入。
- **退出策略**：
  - **止损**：设定在入场点以下0.5%，以限制潜在亏损。
  - **市场条件**：按当前市场价格执行交易，确保对形态识别的快速响应。

### 风险管理

- **仓位规模**：策略根据当前市场状况和交易者的风险偏好，采用动态仓位规模。
- **止损策略**：严格的[止损](https://doc.stocksharp.com/topics/designer/strategies/using_visual_designer/elements/common/protect_position.html)机制，防范不可预见的市场逆转。

## 实施详情

- **平台**：在 StockSharp 平台上实施，利用其强大的 API 进行实时数据处理和订单执行。
- **使用的指标**：结合K线形态识别与成交量分析，提升交易信号的准确性。

## 结论

"Bullish8020"策略为交易者提供了一个强大的工具，用于捕捉市场中特定看涨形态的机会。该策略旨在从强势看涨形态中实现最大收益，同时采用严格的风险管理协议保护投资。
