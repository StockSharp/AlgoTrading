# Exp ATR Trailing 策略

此示例展示如何使用 **平均真实波幅 (ATR)** 指标为已有头寸设置波动性跟踪止损。策略本身不产生入场信号，只根据市场波动调整离场水平。

## 工作原理

1. 订阅指定周期的K线数据。
2. 在每根K线上计算 `AverageTrueRange` 指标。
3. 多头持仓的止损移动到 `收盘价 - ATR * BuyFactor`。
4. 空头持仓的止损移动到 `收盘价 + ATR * SellFactor`。
5. 价格突破跟踪止损时，以市价平仓。

跟踪止损仅朝有利方向移动，不会后退，从而提供基于波动性的退出机制。

## 参数

| 名称 | 说明 |
| --- | --- |
| `AtrPeriod` | ATR 的计算周期。 |
| `BuyFactor` | 多头跟踪止损时使用的 ATR 倍数。 |
| `SellFactor` | 空头跟踪止损时使用的 ATR 倍数。 |
| `CandleType` | 使用的K线时间框架。 |

## 使用说明

- 将策略附加到某个证券上，并手动或通过其他策略开仓。
- 适用于需要将离场管理与入场分离的风险管理场景。
- 图表区域会显示K线、ATR值以及成交交易，便于分析。

## 参考资料

- [Average True Range 指标文档](https://doc.stocksharp.com/topics/indicator_average_true_range.html)
- [Strategy Designer](https://doc.stocksharp.com/topics/designer.html)
