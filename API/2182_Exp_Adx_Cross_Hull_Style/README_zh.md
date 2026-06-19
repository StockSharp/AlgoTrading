# Exp ADX Cross Hull Style 策略
[English](README.md) | [Русский](README_ru.md)

该策略将平均趋向指数（ADX）的交叉信号与 Hull 移动平均 (HMA) 滤波相结合。当 +DI 上穿 -DI 时开多单；当 -DI 上穿 +DI 时开空单。平仓由一对 Hull 移动平均线控制：快速 HMA 下穿慢速 HMA 时平多仓，快速 HMA 上穿慢速 HMA 时平空仓。默认使用 4 小时周期。

## 细节
- **入场条件**  
  - **多头**：+DI 上穿 -DI。  
  - **空头**：-DI 上穿 +DI。
- **出场条件**  
  - **多头**：快速 HMA 低于慢速 HMA。  
  - **空头**：快速 HMA 高于慢速 HMA。
- **指标**  
  - AverageDirectionalIndex，周期 14。  
  - HullMovingAverage 快线 20。  
  - HullMovingAverage 慢线 50。
- **周期**：4 小时 K 线（可调整）。
- **止损**：默认无。
- **方向**：做多与做空。

策略基于实时蜡烛数据运行，不依赖历史集合。参数可根据不同市场进行优化。图表上绘制价格蜡烛、两条 HMA 以及交易标记。
