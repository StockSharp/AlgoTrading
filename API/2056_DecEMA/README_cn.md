# DecEMA 策略
[Русский](README_ru.md) | [English](README.md)

该策略基于 DecEMA 指标来跟随趋势方向。DecEMA 通过十次连续的指数平滑并按特定系数组合，得到低滞后的移动平均线。策略比较最近三个 DecEMA 值：当曲线转向上并且当前值高于前一个值时买入并关闭空头；当曲线转向下并且当前值低于前一个值时卖出并关闭多头。

## 详情

- **入场条件**:
  - 多头：DecEMA 斜率向上，当前值 > 前值
  - 空头：DecEMA 斜率向下，当前值 < 前值
- **做多/做空**: 都支持
- **出场条件**:
  - 多头：斜率转为向下
  - 空头：斜率转为向上
- **止损**: 无
- **默认参数**:
  - `EmaPeriod` = 3
  - `Length` = 15
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: DecEMA
  - 止损: 否
  - 复杂度: 中等
  - 时间框架: 中期
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
