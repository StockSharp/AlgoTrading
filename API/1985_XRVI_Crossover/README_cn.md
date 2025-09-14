# XRVI Crossover 策略
[English](README.md) | [Русский](README_ru.md)

XRVI Crossover 策略基于扩展的相对活力指数 XRVI。
XRVI 通过对 RVI 进行平滑处理并再次应用移动平均来生成信号线。
当 XRVI 向上穿越信号线时做多，向下穿越时做空。
出现反向信号时会反转持仓。

## 细节

- **入场条件**：XRVI 与信号线的交叉
- **多/空**：双向
- **出场条件**：反向交叉
- **止损**：无
- **默认参数**：
  - `RviLength` = 10
  - `SignalLength` = 5
  - `CandleType` = H4 时间框
- **过滤器**：
  - 分类：震荡指标
  - 方向：双向
  - 指标：Relative Vigor Index, Simple Moving Average
  - 止损：无
  - 复杂度：基础
  - 时间框：日内
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
