# Hull Suite No SL/TP
[English](README.md) | [Русский](README_ru.md)

Hull Suite No SL/TP 是一种基于 Hull 移动平均变体的趋势跟随策略。当 Hull 线相对于两根K线前的值改变方向时，策略会反转持仓。

## 细节
- **数据**: 价格K线。
- **入场条件**:
  - **做多**: Hull 值高于两根K线前。
  - **做空**: Hull 值低于两根K线前。
- **出场条件**: 反向信号。
- **止损**: 无。
- **默认参数**:
  - `Length` = 55
  - `Mode` = `Hma`
- **过滤器**:
  - 分类: 趋势跟随
  - 方向: 多头和空头
  - 指标: Hull Moving Average
  - 复杂度: 低
  - 风险级别: 低
