# True Scalper Profit Lock 策略

## 概述

True Scalper Profit Lock 策略是 MetaTrader 4 智能交易系统 **TrueScalperProfitLock.mq4** 的 StockSharp 版本。策略结合了 3 周期与 7 周期指数移动平均线的交叉，以及两种可选的 RSI 极性过滤器，用于在高频剥头皮环境中寻找反转机会。仓位建立后会自动设置止损、止盈，并可选择在达到预设利润时把止损上调到保本区间。

## 交易逻辑

- **趋势判定：** 使用上一根已完成 K 线的收盘价计算 3 周期与 7 周期 EMA。当快速 EMA 与慢速 EMA 的差值大于一个最小价格步长时，视为存在有效趋势。
- **RSI 过滤：** 策略保留了原始 EA 的两种校验模式。方法 A 要求 2 周期 RSI 在最近两根 K 线之间穿越阈值；方法 B 仅检测两根 K 线之前的 RSI 是否高于或低于阈值。两种模式可以独立启用，默认开启方法 B。
- **入场方向：** 做多需要快速 EMA 高于慢速 EMA，同时 RSI 显示超卖（低于阈值）；做空则要求 RSI 显示超买（高于阈值）。

## 仓位管理

- **初始防护：** 入场后会根据合约的最小报价单位计算固定距离的止损和止盈（默认分别为 90 点和 44 点）。
- **利润锁定：** 当价格向有利方向移动超过 `BreakEvenTriggerPoints` 时，可选择将止损上移到开仓价附近，再加上 `BreakEvenOffsetPoints` 指定的偏移，复制 EA 中的 ProfitLock 功能。
- **放弃机制：** `AbandonBars` 控制放弃计时的长度。方法 A 在超时后平仓并立即设置反向入场标志，方法 B 则仅关闭仓位并重新等待信号。
- **资金管理：** 资金管理公式与原版一致，根据账户权益、风险百分比以及迷你账户/真实账户限制计算下单手数。关闭 `UseMoneyManagement` 时会回退到固定下单量。

## 主要参数

| 参数 | 说明 |
|------|------|
| `CandleType` | 处理的 K 线周期。 |
| `FixedVolume` | 关闭资金管理时的基础下单量。 |
| `TakeProfitPoints` / `StopLossPoints` | 止盈与止损的距离（以最小价位计）。 |
| `UseRsiMethodA` / `UseRsiMethodB` | 是否启用两种 RSI 过滤模式。 |
| `RsiThreshold` | RSI 比较使用的阈值。 |
| `AbandonMethodA` / `AbandonMethodB` | 放弃机制的两种行为。 |
| `AbandonBars` | 放弃机制启动前需完成的 K 线数量。 |
| `UseMoneyManagement`、`RiskPercent`、`AccountIsMini`、`LiveTradingMode` | 资金管理相关配置。 |
| `UseProfitLock`、`BreakEvenTriggerPoints`、`BreakEvenOffsetPoints` | 保本移动设置。 |
| `MaxOpenTrades` | 允许同时持有的最大仓位数量。 |

## 使用提示

1. 策略仅使用已完成的 K 线，与原始 EA 的 `shift` 逻辑保持一致。
2. 通过启用或禁用两种 RSI 模式可以复制原始配置。
3. 保本和放弃机制依赖于蜡烛的最高价/最低价来判断是否触发，在高周期下运行时需考虑跳空造成的影响。
4. 资金管理需要投资组合提供 `BeginValue`，否则会自动退回固定下单量。

## 文件

- `CS/TrueScalperProfitLockStrategy.cs` – 策略的 C# 实现。
- `README.md` – 英文说明。
- `README_ru.md` – 俄文说明。

