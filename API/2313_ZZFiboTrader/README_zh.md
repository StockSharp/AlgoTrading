# ZZFibo Trader 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

利用 ZigZag 转折点计算斐波那契回撤水平。每当出现新的高点或低点时重新计算这些水平。当价格在当前趋势方向上突破 50% 回撤并超过设定的突破距离时开仓。可通过绝对价差参数启用止损保护。

## 参数
- K线类型
- Breakout（突破距离）
- Stop Loss（止损距离）
- ZigZag Depth（ZigZag 深度）
