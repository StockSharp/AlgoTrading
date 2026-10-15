# Revolution Volatility Bands With Range Contraction Signal VII 策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略使用指数移动平均线构建包络通道，并寻找上下轨之间距离收缩的时期。当检测到收缩且价格突破平滑的上下轨时，策略顺势开仓。

## 详情

- **入场条件**:
  - **做多**：区间收缩且收盘价突破上轨。
  - **做空**：区间收缩且收盘价跌破下轨。
- **出场条件**：相反方向的突破。
- **指标**：基于 EMA 的包络线。
- **时间框架**：任意。
