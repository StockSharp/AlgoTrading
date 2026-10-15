# 移动平均线交叉策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略跟踪快速与慢速指数移动平均线之间的关系。向上交叉时开立或反转为多头仓位，向下交叉时开立或反转为空头仓位。信号仅在 K 线收盘后计算。

## 详情

- **多头入场**：快速 EMA 从下向上穿过慢速 EMA。
- **空头入场**：快速 EMA 从上向下穿过慢速 EMA。
- **退出**：反向交叉会反转仓位；百分比止损可提前平仓。
- **默认值**：
  - `FastLength` = 100
  - `SlowLength` = 400
  - `StopLossPercent` = 2
  - `CandleType` = 1 分钟
- **实现**：C# 和 Python。
