# Gartley 222 策略
[English](README.md) | [Русский](README_ru.md)

该策略在形成看涨 Gartley 222 形态时做多。
形态通过枢轴高低点并结合斐波那契比例进行验证。

当价格收于 C 点之上并经过 `PivotLength` 根K线延迟后入场。
保护机制使用斐波那契扩展的止盈和百分比止损退出。

## 详情

- **入场条件**：
  - Gartley 222 看涨形态确认
  - 入场延迟 `PivotLength` 根K线
- **多空方向**：仅做多
- **出场条件**：
  - 止盈或止损
- **止损**：
  - 入场价下方 `Stop Loss %`
  - 入场价上方 `TP Fib Extension`
- **默认值**：
  - `Pivot Length` = 5
  - `Fib Tolerance` = 0.05
  - `TP Fib Extension` = 1.27
  - `Stop Loss %` = 2

- **筛选器**：
  - 分类：形态
  - 方向：多头
  - 指标：枢轴、斐波那契
  - 止损：是
  - 复杂度：中等
  - 时间框架：中等
  - 季节性：无
  - 神经网络：否
  - 背离：否
  - 风险等级：中等
