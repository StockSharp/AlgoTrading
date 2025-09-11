# Fibonacci TP SL 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用斐波那契回撤水平进行入场，采用 ATR 止损和百分比止盈。交易受限于最小的交易间隔和每周收益上限。

## 细节

- **入场条件**：
  - **做多**：`Close <= Fib 38.2%` && `Close >= Fib 78.6%` && `距离上次交易的最小柱数`
  - **做空**：`Close <= Fib 23.6%` && `Close >= Fib 61.8%` && `距离上次交易的最小柱数`
- **方向**：双向
- **出场条件**：
  - `ATR 止损` 或 `止盈`
- **止损**：是
- **默认参数**：
  - `Take Profit %` = 4
  - `Min Bars Between Trades` = 10
  - `Lookback` = 100
  - `ATR Period` = 14
  - `ATR Multiplier` = 1.5
  - `Max Weekly Return` = 0.15

- **过滤器**：
  - 类别：均值回归
  - 方向：双向
  - 指标：Highest、Lowest、ATR
  - 止损：是
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
