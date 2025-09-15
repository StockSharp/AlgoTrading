# X2MA JFATL 交叉策略
[English](README.md) | [Русский](README_ru.md)

该策略是 MetaTrader 专家 `Exp_X2MA_JFatl` 的 StockSharp 版本。它结合了快速的简单移动平均线 (SMA)、较慢的 Jurik 移动平均线 (JMA) 以及额外的 JMA 过滤器来确认趋势方向。当快速均线向上穿越慢速均线且价格位于过滤器之上时开多单；当快速均线向下穿越慢速均线且价格位于过滤器之下时开空单。若价格反向穿过过滤器或出现相反的均线交叉，头寸将被关闭。

## 细节

- **入场条件**：
  - **做多**：`SMA_fast` 上穿 `JMA_slow` 且 `Close` > `JMA_filter`。
  - **做空**：`SMA_fast` 下穿 `JMA_slow` 且 `Close` < `JMA_filter`。
- **出场条件**：
  - 价格移动到过滤器的另一侧。
  - 均线发生相反交叉。
- **多空方向**：双向。
- **止损**：默认不使用。
- **默认值**：
  - `Fast MA Length` = 5。
  - `Slow MA Length` = 12。
  - `Filter Length` = 20。
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：多个（SMA，JMA）
  - 止损：无
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
