# J-Lines Ribbon 4-Cycle Engine 策略
[English](README.md) | [Русский](README_ru.md)

J-Lines Ribbon 4-Cycle Engine 策略通过EMA带和平均方向指数将市场划分为 CHOP、LONG 和 SHORT 周期。策略在新的周期出现以及价格从关键 EMA 反弹时入场，在相反的交叉或摆动突破时平仓。

## 细节

- **入场条件**：
  - **多头**：出现新的 LONG 周期或在 EMA72/EMA126 上方反弹且 EMA72 > EMA89。
  - **空头**：出现新的 SHORT 周期或在 EMA72/EMA126 下方反弹且 EMA72 < EMA89。
- **止损**：最近摆动高/低点。
- **默认参数**：
  - `DmiLength` = 8
  - `AdxFloor` = 12
- **过滤器**：
  - 类型：趋势
  - 方向：双向
  - 指标：EMA, ADX
  - 止损：是
  - 复杂度：中等
  - 时间框架：任意
  - 季节性：无
  - 神经网络：无
  - 背离：无
  - 风险等级：中等
