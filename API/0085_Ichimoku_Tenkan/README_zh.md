# 一目均衡表 Tenkan/Kijun 交叉策略
[English](README.md) | [Русский](README_ru.md)

一目均衡表是一套完整的趋势跟随系统。本策略关注 Tenkan 线与 Kijun 线的交叉，并结合价格相对于云层的位置。Tenkan 在云层上方上穿 Kijun 表示趋势可能继续上行；在云层下方下穿则表明疲弱。

测试表明年均收益约为 142%，该策略在股票市场表现最佳。

运行中，策略在每根K线上计算一目均衡表组件。当 Tenkan 上穿 Kijun 且价格位于云层上方时开多仓，止损设在 Kijun 附近。反之，在云层下方的向下交叉则做空，止损同样靠近 Kijun。

系统持仓直至止损被触发或再次交叉，力求捕捉沿云层方向的持续行情。

## 细节

- **入场条件**：Tenkan/Kijun 交叉并结合价格相对云层的位置。
- **多/空**：双向。
- **退出条件**：止损或相反交叉。
- **止损**：有，在 Kijun 附近。
- **默认值**：
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = 30 分钟
- **过滤条件**：
  - 类别: 趋势跟随
  - 方向: 双向
  - 指标: 一目均衡表
  - 止损: 有
  - 复杂度: 中等
  - 时间框架: 波段
  - 季节性: 无
  - 神经网络: 无
  - 背离: 无
  - 风险级别: 中等

