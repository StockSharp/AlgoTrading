# Bulls & Bears Power Cross 策略
[English](README.md) | [Русский](README_ru.md)

该策略在四小时周期上利用 Bulls Power 与 Bears Power 指标的交叉进行交易。Bulls Power 衡量价格上方的买方力量，Bears Power 衡量价格下方的卖方力量。当买方力量超过卖方力量时，系统做多；当卖方力量占优时，系统做空。

在加密货币历史数据上的测试表明，这些指标交叉往往预示短期反转。策略始终保持持仓，并在每次新的交叉时反转方向。

## 细节

- **入场条件**：
  - **做多**：Bulls Power 从下向上穿越 Bears Power。
  - **做空**：Bears Power 从下向上穿越 Bulls Power。
- **多空方向**：双向。
- **出场条件**：相反的交叉触发仓位反转。
- **止损**：无，仓位通过反向信号关闭。
- **过滤器**：
  - 时间框架：默认 4 小时 K 线。
  - 指标：Bulls Power、Bears Power。
  - 方向：基于动量变化的反转。
