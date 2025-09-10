# 山寨币指数相关策略
[English](README.md) | [Русский](README_ru.md)

该策略比较交易品种和参考指数的 EMA 趋势。当两个品种的快 EMA 均高于慢 EMA 时做多，均低于慢 EMA 时做空。可选择反向逻辑或忽略指数。

## 详情

- **入场条件**：
  - 两个品种的快 EMA 高于慢 EMA（反向逻辑时相反）。
- **多空方向**：双向。
- **出场条件**：
  - EMA 反向交叉。
- **止损**：无。
- **默认值**：
  - `FastEmaLength` = 47
  - `SlowEmaLength` = 50
  - `IndexFastEmaLength` = 47
  - `IndexSlowEmaLength` = 50
  - `SkipIndexReference` = false
  - `InverseSignal` = false
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：EMA
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中
