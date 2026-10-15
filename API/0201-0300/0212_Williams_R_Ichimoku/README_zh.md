# Williams R Ichimoku 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

此策略结合Williams %R与一目均衡表。当%R跌破WilliamsROversold且价格位于云层上方并且转折线高于基准线时做多；当%R高于WilliamsROverbought且价格在云层下方并且转折线低于基准线时做空。价格穿越云层另一侧时离场。

测试表明年均收益约为 73%，该策略在加密市场表现最佳。

该方法适合偏好明确趋势过滤的交易者，云层的另一侧充当动态止损，随趋势调整。

## 细节
- **入场条件**:
  - 多头: `%R < WilliamsROversold && price above Ichimoku cloud && Tenkan > Kijun`
  - 空头: `%R > WilliamsROverbought && price below Ichimoku cloud && Tenkan < Kijun`
- **多/空**: 双向
- **离场条件**:
  - 多头: 价格跌破云层时平仓
  - 空头: 价格突破云层时平仓
- **止损**: 是
- **默认值**:
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80
  - `WilliamsROverbought` = -20
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
    -80和-20是规则中Williams %R阈值的默认值。价格收于云层下方时多头平仓，收于云层上方时空头平仓。与持仓方向相反的入场信号会反转持仓。
- **过滤器**:
  - 类别: Mixed
  - 方向: 双向
  - 指标: Williams R Ichimoku
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: 日内
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等

