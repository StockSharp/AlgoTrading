# Higher Order Pivots 策略
[English](README.md) | [Русский](README_ru.md)

使用 3 根或 5 根 K 线定义的枢轴点来检测一、二、三阶高低点。该策略仅用于分析，不会下单。

## 详情

- **入场条件**：
  - 无（仅分析）。
- **出场条件**：
  - 无。
- **指标**：
  - 3 或 5 根 K 线的枢轴点检测。
- **止损**：无。
- **默认值**：
  - `CandleType` = 5m
  - `UseThreeBar` = true
  - `DisplayFirstOrder` = true
  - `DisplaySecondOrder` = true
  - `DisplayThirdOrder` = true
- **过滤器**：
  - 单一时间框
  - 指标：枢轴检测
  - 止损：无
  - 复杂度：低
