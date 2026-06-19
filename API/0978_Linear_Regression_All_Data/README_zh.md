# Linear Regression (All Data) 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用所有可用K线计算线性回归并在图表上绘制，同时记录斜率、截距和相关系数。

## 细节

- **入场条件**：无。
- **做多/做空**：无。
- **出场条件**：无。
- **止损**：否。
- **默认值**：
  - `MaxBarsBack` = 5000。
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()。
- **筛选**：
  - 分类：工具
  - 方向：无
  - 指标：Linear Regression
  - 止损：否
  - 复杂度：基础
  - 时间框架：任意
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险等级：低
