# ETH/USDT EMA 交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略利用 EMA 交叉并结合多个过滤条件交易 ETH/USDT。

当 20 周期 EMA 上穿 50 周期 EMA，且价格高于 200 周期 EMA、RSI 大于 30、ATR 高于其移动平均并且成交量大于平均值时建立多头；反向条件下建立空头。

出现反向信号时平仓反手，不使用固定止损或止盈。

## 细节

- **入场条件**：
  - **多头**：`EMA20 上穿 EMA50` && `Close > EMA200` && `RSI > 30` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
  - **空头**：`EMA20 下穿 EMA50` && `Close < EMA200` && `RSI < 70` && `ATR > SMA(ATR,10)` && `Volume > SMA(Volume,20)`
- **方向**：双向
- **出场条件**：
  - 反向信号
- **止损**：无
- **默认参数**：
  - `EMA200 Length` = 200
  - `EMA20 Length` = 20
  - `EMA50 Length` = 50
  - `RSI Length` = 14
  - `ATR Length` = 14

- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：EMA、RSI、ATR
  - 止损：无
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：中等
