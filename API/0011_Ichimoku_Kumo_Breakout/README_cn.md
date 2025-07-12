# Ichimoku云突破

该策略依靠Ichimoku云形态信号。当价格突破云层并且转折线(Tenkan-sen)上穿基准线(Kijun-sen)时买入，反向突破则做空，直到价格返回云层内。云层给出关键支撑与阻力，多重Ichimoku组件可避免震荡期的低概率交易。

## 详情
- **入场条件**: 基于 Ichimoku 信号
- **多空方向**: 双向
- **退出条件**: 反向信号
- **止损**: 无
- **默认值**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(15)
- **过滤器**:
  - 类型: 突破
  - 方向: 双向
  - 指标: Ichimoku
  - 止损: 无
  - 复杂度: 基础
  - 时间框架: 日内 (15m)
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险等级: 中
