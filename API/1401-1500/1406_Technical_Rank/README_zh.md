# 技术排名
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

该策略结合多种技术指标计算综合排名。当排名高于上阈值时做多，低于下阈值时做空。

## 细节

- **入场条件**：排名 > 上阈值 -> 做多；排名 < 下阈值 -> 做空
- **方向**：双向
- **出场条件**：相反信号
- **止损**：无
- **默认值**：
  - `UpperThreshold` = 70
  - `LowerThreshold` = 30
  - `CandleType` = 1 分钟K线
