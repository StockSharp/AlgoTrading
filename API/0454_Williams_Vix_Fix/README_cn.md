# Williams VIX Fix策略
[English](README.md) | [Русский](README_ru.md)

Williams VIX Fix策略将Larry Williams的波动率指标应用于没有官方VIX的品种。该指标通过计算过去一段时间最高收盘价与当前最低价之间的距离来构造合成VIX。当该值上穿基于布林带的阈值或价格跌破下轨时，被视为超卖信号；通过反向计算可以识别超买情形。

该方法寻找波动率冲击后的均值回归。当VIX Fix显示极度恐慌且价格低于下轨时开多仓；当反向VIX Fix提示极度乐观且价格高于上轨时，平掉现有多仓。百分位阈值用于调节敏感度。

## 细节

- **入场条件**：
  - VIX Fix ≥ 上轨或百分位，且价格 < 布林带下轨。
- **方向**：仅做多，反向信号用于平仓。
- **出场条件**：
  - 反向VIX Fix ≥ 上轨或百分位，且价格 > 布林带上轨。
- **止损**：无。
- **默认参数**：
  - `BbLength` = 20
  - `BbMultiplier` = 2.0
  - `WvfPeriod` = 20
  - `WvfLookback` = 50
  - `HighestPercentile` = 0.85
  - `LowestPercentile` = 0.99
- **过滤器**：
  - 类型：波动率均值回归
  - 方向：多头
  - 指标：布林带、Williams VIX Fix
  - 止损：无
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
