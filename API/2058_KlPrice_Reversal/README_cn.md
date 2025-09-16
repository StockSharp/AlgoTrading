# KlPrice Reversal 策略

该策略是 MQL5 专家 **exp_i-KlPrice.mq5** 的 C# 版本。它基于归一化价格振荡指标实现反转交易系统。振荡器将当前价格与由移动平均线和平均真实波幅（ATR）构建的平滑价格带进行比较。穿越预设边界会产生交易信号。

## 工作原理

1. 简单移动平均线（SMA）平滑收盘价。
2. 平均真实波幅（ATR）评估市场波动。
3. 振荡器计算公式：
   
   `jres = 100 * (Close - (SMA - ATR)) / (2 * ATR) - 50`
4. 振荡器数值分为五个区域：
   - **4** – 高于上方阈值
   - **3** – 介于零和上方阈值之间
   - **2** – 介于上方和下方阈值之间
   - **1** – 介于下方阈值和零之间
   - **0** – 低于下方阈值
5. 当振荡器离开区域 4 时开多单；离开区域 0 时开空单；当振荡器穿越零轴时平仓。

## 参数

| 名称 | 描述 |
|------|------|
| `CandleType` | 计算所用的K线周期。 |
| `PriceMaLength` | 平滑价格的 SMA 周期。 |
| `AtrLength` | ATR 周期，用于估算价格带。 |
| `UpLevel` | 振荡器上方阈值。 |
| `DownLevel` | 振荡器下方阈值。 |
| `EnableBuy` | 允许开多头。 |
| `EnableSell` | 允许开空头。 |

## 使用方法

1. 创建 `KlPriceReversalStrategy` 实例。
2. 设置所需参数。
3. 将策略连接到投资组合和证券。
4. 启动策略以接收信号并发送订单。

策略通过 `BuyMarket` 和 `SellMarket` 提交市价单，并通过 `StartProtection()` 启用仓位保护。

## 说明

- 实现使用 StockSharp 内置指标 `SimpleMovingAverage` 和 `AverageTrueRange`，以近似原始 MQL 指标。
- 所有计算仅基于已完成的K线。
