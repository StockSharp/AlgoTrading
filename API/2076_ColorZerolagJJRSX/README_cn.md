# 颜色零延迟 JJRSX 策略

该策略复制 MetaTrader 中 **ColorZerolagJJRSX** 交易策略的逻辑，使用两条平滑处理的 RSI 线来接近原始指标。快线和慢线的交叉用于生成交易信号。

## 工作原理

- 当快线从慢线下方突破时，策略关闭所有空仓且可选择开立一笔多仓。
- 当快线从慢线上方突破时，策略关闭所有多仓且可选择开立一笔空仓。
- 使用 `StartProtection` 机制设置止损和目标价目标。

## 参数

| 名称 | 描述 |
| --- | --- |
| `FastPeriod` | JJRSX 快线周期 |
| `SlowPeriod` | JJRSX 慢线周期 |
| `BuyOpen` | 是否允许开多 |
| `SellOpen` | 是否允许开空 |
| `BuyClose` | 遇到反向信号时关闭多仓 |
| `SellClose` | 遇到反向信号时关闭空仓 |
| `StopLoss` | 止损点（价格单位） |
| `TakeProfit` | 目标价（价格单位） |
| `CandleType` | 计算使用的K红时间周期 |

## 备注

- 策略使用内置指标和高级 `Bind` API。
- 交易量从策略的 `Volume` 属性获取。
- 本策略不提供 Python 版本。

## 参考

原 MQL 代码位于本仓库的 `MQL/13854`。

