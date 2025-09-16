# Stochastic Automated 策略

该策略在选定的K线周期上使用 **Stochastic 振荡器**。当 %K 与 %D 进入极端区域并发生交叉时触发交易。每笔交易都设置固定的止盈和止损，并使用移动止损来锁定利润。

## 逻辑

1. **入场**
   - **做多：**
     - 两根K线之前，%K 与 %D 都低于 `OverSold`。
     - 两根K线之前 %D 在 %K 之上，而一根K线之前在 %K 之下。
     - %D 正在上升。
   - **做空：**
     - 两根K线之前，%K 与 %D 都高于 `OverBought`。
     - 两根K线之前 %D 在 %K 之下，而一根K线之前在 %K 之上。
     - %D 正在下降。
2. **出场**
   - 当 Stochastic 脱离极端区域或 %D 反转时平仓。
   - 如果价格回撤超过 `TrailingStop`，移动止损将退出头寸。
   - 每笔交易都应用全局 `TakeProfit` 与 `StopLoss`。

## 参数

| 名称 | 说明 |
|------|------|
| `CandleType` | Stochastic 计算所用的K线周期。 |
| `KPeriod` | %K 的回溯周期。 |
| `DPeriod` | %D 的平滑周期。 |
| `Slowing` | %K 的附加平滑。 |
| `OverBought` | 超买阈值。 |
| `OverSold` | 超卖阈值。 |
| `TakeProfit` | 入场到止盈的距离（价格单位）。 |
| `StopLoss` | 入场到止损的距离（价格单位）。 |
| `TrailingStop` | 移动止损的距离（价格单位）。 |

## 指标

- `StochasticOscillator`

## 备注

- 代码注释为英文。
- 暂无 Python 版本。
