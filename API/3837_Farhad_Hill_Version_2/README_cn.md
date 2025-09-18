# Farhad Hill Version 2 策略 (C#)

## 概述
本策略是 MetaTrader 专家顾问 “Farhad Hill Version 2” 的 StockSharp 版本。
它结合多个指标过滤器，在外汇品种上捕捉趋势反转。移植保留了原始指标组合（MACD、
Stochastic、Parabolic SAR、Momentum 以及可选的均线交叉），并实现了资金管理和
移动止损的全部逻辑。

策略使用单一时间框（默认 30 分钟 K 线），同一时间仅持有一笔仓位。与 MQL 版本一样，
支持止损、止盈以及三种不同的跟踪止损模式。按照仓库要求，代码中的注释全部为英文。

## 交易逻辑
- **MACD 过滤器** – 启用后，多头要求 MACD 主线低于信号线，空头要求主线高于信号线。
- **随机指标区间过滤器** – 多头要求 %K 低于下限，空头要求 %K 高于上限。若启用交叉过滤，
  多头必须出现 %K 向上穿越 %D，空头必须出现 %K 向下跌破 %D。
- **抛物线 SAR 过滤器** – 多头要求 SAR 位于价格下方并且向下移动；空头要求 SAR 位于价格上方
  并向上移动。移植版本使用收盘价作为比较基准。
- **Momentum 过滤器** – 使用开盘价计算，与原 EA 设置一致。多头要求动量低于下限，空头要求
  动量高于上限。
- **均线交叉（可选）** – 可配置均线类型、价格类型和周期。多头需要快线在慢线之上，空头相反。

策略仅在完整 K 线收盘时评估信号，如果已有仓位则不会开立新仓。下单采用计算好的
市场单手数。

## 仓位管理
- **止损 / 止盈** – 以点（pip）为单位设置。点值来源于合约的 `PriceStep`，若不可用则默认 0.0001。
- **跟踪止损模式**
  1. 即时模式 – 一旦价格突破止损距离，止损立即跟随价格。
  2. 延迟模式 – 先等待价格相对入场价移动指定距离，再按固定偏移跟随。
  3. 三阶段模式 – 复刻原策略的三段式逻辑，包含两次保本调整和最终的移动止损。
- 防护委托使用 `SellStop`/`BuyStop`（止损）和 `SellLimit`/`BuyLimit`（止盈）发送，以便在交易所可见。

## 资金管理
- **固定手数** – 直接使用设定的固定手数。若启用 `AccountIsMini`，则自动换算成最小 0.1 手的迷你手。
- **百分比风险** – 按原策略公式
  `floor(FreeMargin * percent / 10000) / 10` 计算手数，并受 `MaxLots` 限制，同时考虑迷你账户。
  若无法获取投资组合价值，则回退到固定手数。

## 参数
所有参数都通过 `StrategyParam<T>` 暴露，可在界面中调整或用于优化。主要分组如下：

| 分组 | 参数 | 说明 |
| --- | --- | --- |
| General | `CandleType` | 信号使用的 K 线周期 |
| Money Management | `AccountIsMini`, `UseMoneyManagement`, `TradeSizePercent`, `FixedVolume`, `MaxLots` |
| Risk | `StopLossPips`, `TakeProfitPips`, `UseTrailingStop`, `TrailingStopType`, `TrailingStopPips`, `FirstMovePips`, `TrailingStop1`, `SecondMovePips`, `TrailingStop2`, `ThirdMovePips`, `TrailingStop3` |
| Indicators | `UseMacd`, `UseStochasticLevel`, `UseStochasticCross`, `UseParabolicSar`, `UseMomentum`, `UseMovingAverageCross`, `MacdFast`, `MacdSlow`, `MacdSignal`, `StochasticK`, `StochasticD`, `StochasticSlowing`, `StochasticHigh`, `StochasticLow`, `MomentumPeriod`, `MomentumHigh`, `MomentumLow`, `SlowMaPeriod`, `FastMaPeriod`, `MaMode`, `MaPrice` |

## 说明与假设
- Parabolic SAR 判断基于收盘价，以保证历史回测的确定性。
- 使用资金管理时，需要连接投资组合以读取权益；否则回退到固定手数模式。
- 所有指标信号均在 K 线收盘后评估，避免半成品数据带来的误触发。

## 文件
- `CS/FarhadHillVersion2Strategy.cs` – 策略的 C# 实现。
- `README.md` – 英文文档。
- `README_ru.md` – 俄文文档。
- `README_cn.md` – 本中文文档。
