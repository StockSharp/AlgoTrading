# Slope Direction Line 策略
[English](README.md) | [Русский](README_ru.md)

该策略复现 *Slope Direction Line* 智能交易系统的逻辑。它根据收盘价线性回归的斜率进行交易。当斜率由负转正时开多仓，斜率由正转负时开空仓。每次方向变化时都会平掉相反方向的仓位。通过 `StartProtection` 设置的止损和止盈百分比来保护仓位。

## 细节
- **指标** – 使用 StockSharp 的 `LinearRegression`，信号来自 `LinearRegSlope` 分量。
- **信号** – 斜率穿越零轴。
- **进出场** – 斜率改变符号时平掉当前仓位并按新方向入场（若允许）。
- **风险控制** – `StartProtection` 按百分比设置止损和止盈。

## 参数
| 名称 | 说明 |
|------|------|
| `CandleType` | 构建蜡烛图的时间框架。 |
| `Length` | 回归计算所用的柱数。 |
| `TakeProfitPercent` | 从入场价计算的止盈百分比。 |
| `StopLossPercent` | 从入场价计算的止损百分比。 |
| `AllowLong` | 允许开多。 |
| `AllowShort` | 允许开空。 |

## 使用方法
1. 将策略添加到 StockSharp 应用程序。
2. 根据需求设置参数和风险。
3. 启动策略并在图表上监控交易。

