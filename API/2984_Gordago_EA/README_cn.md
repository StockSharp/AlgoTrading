# Gordago EA 策略
[English](README.md) | [Русский](README_ru.md)

这是对 MetaTrader 5 中经典的 “Gordago EA” 专家的移植版本。策略在基础周期（默认 3 分钟）上执行交易，同时在更高周期上读取 MACD（默认 12 分钟）和随机指标（默认 1 小时）作为过滤条件。所有原始的止损/止盈和跟踪止损设定都被保留，并使用 StockSharp 的高级 API 完成订阅与下单。

## 策略逻辑

- **数据来源**
  - 执行蜡烛：可配置，默认三分钟。
  - MACD 蜡烛：可配置，默认十二分钟。
  - 随机指标蜡烛：可配置，默认一小时。
- **指标配置**
  - MACD：快速 12、慢速 26、信号 9。
  - 随机指标：长度 5，%K 平滑 3，%D 平滑 3。
- **入场条件**
  - **做多**：当前 MACD 高于前一值且前一值低于零；随机指标 %K 低于买入阈值（默认 37）并且较上一值上升。
  - **做空**：当前 MACD 低于前一值且前一值高于零；随机指标 %K 高于卖出阈值（默认 96）并且较上一值下降。
- **下单规则**
  - 使用固定手数，若方向反转会先平掉反向仓位再开新单。
  - 多头和空头拥有独立的止损/止盈距离（默认多头 40/70 pips，空头 10/40 pips）。
- **离场与风控**
  - 每根完成的基础蜡烛都会检查止损和止盈是否触发。
  - 跟踪止损在价格突破“距离 + 步长”后启动，并持续按设定距离上移/下移。
  - 即使初始止损为零，跟踪逻辑也可以创建保护性止损，与原始 EA 的行为一致。

## 参数

- `OrderVolume` – 交易手数。
- `StopLossBuyPips` / `TakeProfitBuyPips` – 多头止损与止盈距离（pips）。
- `StopLossSellPips` / `TakeProfitSellPips` – 空头止损与止盈距离（pips）。
- `TrailingStopPips` – 跟踪止损距离，设为 0 可关闭。
- `TrailingStepPips` – 跟踪止损每次推进所需的最小额外利润。
- `StochasticBuyLevel` / `StochasticSellLevel` – 随机指标入场阈值。
- `CandleType` – 主执行周期。
- `MacdCandleType` – 计算 MACD 的周期。
- `StochasticCandleType` – 计算随机指标的周期。
- `MacdFastPeriod`、`MacdSlowPeriod`、`MacdSignalPeriod` – MACD 周期。
- `StochasticLength`、`StochasticSignalPeriod`、`StochasticSmoothing` – 随机指标周期参数。

## 实现说明

- pips 会通过品种的 `PriceStep` 转换为价格。如果价格步长带有 3 或 5 位小数，则乘以 10，与原版 MQL 中对 3/5 位报价的调整一致。
- 当设置了正的 `TrailingStopPips` 但 `TrailingStepPips` 小于等于零时，跟踪止损会被忽略，并记录警告。
- 由于基于蜡烛收盘事件运行，保护性逻辑每根蜡烛执行一次，而非逐笔行情，但整体交易逻辑与原始 EA 保持一致。
- 仅提供 C# 版本，本仓库未包含 Python 版本或相应目录。
