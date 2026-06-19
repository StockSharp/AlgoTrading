# Knux 多指标策略
[English](README.md) | [Русский](README_ru.md)

该策略结合趋势强度与动量振荡指标来捕捉突破。当快速移动平均线上穿或下穿慢速移动平均并且ADX显示趋势强劲时，系统会进一步检查RVI、CCI和Williams %R，以确认动量方向且市场未处于极端状态。

策略可做多也可做空，直到出现相反信号为止。默认情况下不使用固定止损，所有参数（包括周期与阈值）均可配置。

## 详情

- **入场条件**：
  - **多头**：快速SMA上穿慢速SMA，`ADX > AdxLevel`，`RVI`上升，`CCI < -CciLevel`，`WPR <= -100 + WprBuyRange`。
  - **空头**：快速SMA下穿慢速SMA，`ADX > AdxLevel`，`RVI`下降，`CCI > CciLevel`，`WPR >= -WprSellRange`。
- **方向**：双向。
- **退出条件**：相反信号（均线反向交叉）。
- **止损**：无固定止损。
- **默认值**：
  - `FastMaLength` = 5
  - `SlowMaLength` = 20
  - `AdxPeriod` = 14
  - `AdxLevel` = 15
  - `RviPeriod` = 20
  - `CciPeriod` = 40
  - `CciLevel` = 150
  - `WprPeriod` = 60
  - `WprBuyRange` = 15
  - `WprSellRange` = 15
- **过滤器**：
  - 类别：趋势跟随
  - 方向：双向
  - 指标：多重
  - 止损：无
  - 复杂度：中等
  - 时间框架：短期
  - 季节性：否
  - 神经网络：否
  - 背离：否
  - 风险级别：中等
