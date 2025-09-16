# Q2MA交叉策略
[Русский](README_ru.md) | [English](README.md)

Q2MA交叉策略基于蜡烛收盘价和开盘价的平滑移动平均线交叉进行交易。当收盘平均线从上方下穿开盘平均线时开多仓，反向交叉时开空仓。当出现相反趋势时平仓，并使用以跳动为单位的止损和止盈。

## 详情

- **入场条件**：收盘价与开盘价移动平均线交叉
- **多空方向**：双向
- **出场条件**：相反交叉或止损/止盈
- **止损**：是
- **默认值**:
  - `Length` = 8
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Volume` = 1
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `Invert` = false
- **过滤器**:
  - 类别: 趋势
  - 方向: 双向
  - 指标: 移动平均
  - 止损: 是
  - 复杂度: 中等
  - 时间框架: H4
  - 季节性: 否
  - 神经网络: 否
  - 背离: 否
  - 风险等级: 中等
