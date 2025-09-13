# MA2CCI
[English](README.md) | [Русский](README_ru.md)

基于CCI确认的均线交叉策略。使用ATR作为止损。

## 详情

- **入场条件**:
  - 快速SMA上穿慢速SMA且CCI上穿0时做多。
  - 快速SMA下穿慢速SMA且CCI下穿0时做空。
- **多/空**: 双向。
- **出场条件**: 反向交叉或距离入场价1倍ATR的止损。
- **止损**: 入场价±ATR。
- **默认值**:
  - `FastMaPeriod` = 4
  - `SlowMaPeriod` = 8
  - `CciPeriod` = 4
  - `AtrPeriod` = 4
  - `CandleType` = 1 分钟
- **筛选**:
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: SMA, CCI, ATR
  - 止损: ATR
  - 复杂度: 初级
  - 时间框架: 日内
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中等
