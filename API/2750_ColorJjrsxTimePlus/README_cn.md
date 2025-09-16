# Color JJRSX Time Plus 策略
[English](README.md) | [Русский](README_ru.md)

本策略由 MetaTrader5 专家 `Exp_ColorJJRSX_Tm_Plus` 改写而来。通过 RSI + Jurik 平滑复制 Color JJRSX 指标的走势，并保留原策略的持仓时间控制与方向开平仓开关。

## 概述

- **核心思想**：检测 Color JJRSX 振荡指标的斜率变化。当斜率向上时平掉空头并可选择做多，斜率向下时平掉多头并可选择做空。
- **交易品种**：连接到策略的单一 `Security`。
- **时间周期**：可配置，默认采用 4 小时 K 线，与原始 EA 参数一致。
- **方向**：支持多空双向，可分别启用/禁用。
- **下单方式**：使用市价单 `BuyMarket()` / `SellMarket()`。

## 指标结构

1. **RSI** —— 基础动量指标，长度由 `RSI Length` 控制（对应 JurXPeriod）。
2. **Jurik Moving Average** —— 对 RSI 输出进行平滑，长度为 `Smoothing Length`（对应 JMAPeriod）。MQL 中的 JMA 相位参数在 StockSharp 中不可用，因此被省略。
3. **Signal Shift** —— 对应原策略的 `SignalBar`，通过回看 `Signal Shift` 根已完成的 K 线以及之前的两根来判断斜率。

## 交易逻辑

### 多头管理
- **开仓**：当开启 `Enable Long Entries` 且振荡器由下行转为上行（`previous < older`）并继续上升（`current > previous`），同时当前仓位<=0 时买入。
- **平仓**：若开启 `Exit Long on Downturn`，一旦斜率再次向下（`previous > older`）即平掉多单。

### 空头管理
- **开仓**：当开启 `Enable Short Entries` 且振荡器由上行转为下行（`previous > older`）并继续下跌（`current < previous`），同时当前仓位>=0 时卖出。
- **平仓**：若开启 `Exit Short on Upturn`，当斜率向上（`previous < older`）时回补空单。

### 时间过滤
- `Enable Time Exit` 控制持仓超出 `Holding Minutes` 后强制平仓，对应原始 EA 的 `nTime` 退出逻辑。

### 风险管理
- `Stop Loss (pts)` 与 `Take Profit (pts)` 转换为 `StartProtection` 的保护单，单位为 `UnitTypes.PriceStep`。

## 参数说明

| 参数 | 含义 | 默认值 |
|------|------|--------|
| `Indicator Timeframe` | 指标使用的 K 线周期。 | 4 小时 |
| `RSI Length` | RSI 周期（JurX）。 | 8 |
| `Smoothing Length` | Jurik 平滑长度（JMA）。 | 3 |
| `Signal Shift` | 信号偏移量（SignalBar）。 | 1 |
| `Enable Long/Short Entries` | 允许做多 / 做空。 | true |
| `Exit Long/Short` | 允许根据斜率退出多 / 空。 | true |
| `Enable Time Exit` | 启用持仓时间限制。 | true |
| `Holding Minutes` | 最大持仓时间（分钟）。 | 240 |
| `Stop Loss (pts)` | 止损点数。 | 1000 |
| `Take Profit (pts)` | 止盈点数。 | 2000 |

## 转换说明

- Color JJRSX 的彩色柱形仅用于判断斜率，因此使用 RSI+Jurik 的组合即可等效完成信号判断。
- 原 EA 的资金管理参数（`MM`、`MMMode`、`Deviation` 等）未迁移。请通过 `Strategy.Volume` 或账户设置控制手数。
- MQL 中依赖全局变量避免重复下单的逻辑在本实现中不需要，因为只在每根已完成的 K 线处运行。
- 按仓库要求，代码与注释全部采用英文。
