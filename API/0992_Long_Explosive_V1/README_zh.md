# Long Explosive V1 策略
[English](README.md) | [Русский](README_ru.md)

当收盘价相对上一根K线上涨达到设定百分比时，Long Explosive V1 开多。若价格下跌超过设定百分比或在新多单前，平掉持仓。

## 详情

- **入场条件**：
  - **多头**：`Close - PrevClose > Close * Price increase (%) / 100`。
- **多/空方向**：仅做多。
- **出场条件**：`Close - PrevClose < -Close * Price decrease (%) / 100` 或新多单之前。
- **止损**：无。
- **默认值**：
  - `Price increase (%)` = 1
  - `Price decrease (%)` = 1
- **筛选器**：
  - 类别：动量
  - 方向：多
  - 指标：价格
  - 止损：无
  - 复杂度：低
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：低
