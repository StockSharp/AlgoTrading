# Ichimoku Clouds Long and Short 策略
[English](README.md) | [Русский](README_ru.md)

该策略使用一目均衡表中的Tenkan-sen和Kijun-sen交叉。根据Tenkan相对于云的位置，交叉被分为强、中性或弱。根据选择的交易模式，当出现所需强度的信号时开多或开空。还可以设置基于百分比的止盈和止损，或者按照相反信号平仓。

## 详情

- **入场条件**:
  - Tenkan-sen上穿Kijun-sen且信号强度符合多头选项。
  - Tenkan-sen下穿Kijun-sen且信号强度符合空头选项。
- **方向**: 可选，默认多头。
- **出场条件**:
  - 按所选退出选项的相反信号。
  - 百分比止盈或止损。
- **止损/止盈**: 百分比止盈和止损。
- **默认值**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `TakeProfitPct` = 0
  - `StopLossPct` = 0
- **过滤器**:
  - 类别: 趋势
  - 方向: 多空皆可
  - 指标: Ichimoku
  - 止损: 可选
  - 复杂度: 中等
  - 时间框架: 任意
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
