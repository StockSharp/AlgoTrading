# Khaled Tamim 的 Avellaneda-Stoikov 策略
[English](README.md) | [Русский](README_ru.md)

实现 Avellaneda-Stoikov 做市模型。策略根据最近两根 K 线的收盘价计算买入和卖出报价，当价格偏离这些报价超过设定阈值时下市价单。

## 详情

- **入场条件**：
  - **做多**：`close < bidQuote - M`
  - **做空**：`close > askQuote + M`
- **多/空**：双向。
- **出场条件**：反向信号。
- **止损**：无。
- **默认值**：
  - `Gamma` = 2
  - `Sigma` = 8
  - `T` = 0.0833
  - `K` = 5
  - `M` = 0.5
  - `Fee` = 0
- **筛选**：
  - 类别：做市
  - 方向：双向
  - 指标：无
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险水平：中
