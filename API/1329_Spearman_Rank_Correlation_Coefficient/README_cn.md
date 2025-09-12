# Spearman Rank Correlation Coefficient 策略
[English](README.md) | [Русский](README_ru.md)

该配对交易策略计算两个证券之间的斯皮尔曼等级相关系数。当相关性高于正阈值时，策略做空第一个证券并做多第二个证券；当相关性低于负阈值时，则执行相反操作。当相关性回到零附近时平仓。

## 详情

- **入场条件：**
  - **做多第一 / 做空第二**：correlation < -Threshold。
  - **做空第一 / 做多第二**：correlation > Threshold。
- **多空方向**：配对交易。
- **出场条件：**
  - 相关性的绝对值 < Threshold / 2。
- **止损**：无。
- **默认参数：**
  - `CorrelationPeriod` = 10
  - `Threshold` = 0.8
- **过滤器：**
  - 类别: Correlation
  - 方向: Both
  - 指标: Spearman Rank Correlation
  - 止损: No
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
