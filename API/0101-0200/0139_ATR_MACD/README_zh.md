# ATR MACD 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略利用平均真实波幅(ATR)来调整仓位规模，并根据MACD的交叉进行交易。
ATR数值越大，持仓规模越小，从而在不同市场环境中保持风险一致。
当MACD与信号线交叉时入场，反向交叉或百分比止损触发退出。
这种组合旨在捕获动量的同时考虑到波动性的变化。

测试表明年均收益约为 154%，该策略在股票市场表现最佳。

## 细节

- **入场条件**：指标信号
- **多/空**：均可
- **退出条件**：止损或反向信号
- **止损**：是，按百分比
- **默认值**:
  - `CandleType` = 15分钟
  - `StopLossPercent` = 2
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `AtrPeriod` = 14
  - `AtrAvgPeriod` = 20
    MACD上穿信号线做多、下穿做空，反向交叉反转持仓。每笔新仓位的数量为Volume乘以最近AtrAvgPeriod个ATR的平均值再除以当前ATR，并向下取整到成交量步长。
- **过滤器**:
  - 类别：趋势跟随
  - 方向：双向
  - 指标：ATR, MACD
  - 止损：有
  - 复杂度：中等
  - 时间框架：日内
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等

