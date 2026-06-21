# 趋势跟随移动均线策略
[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

计算移动平均线并在动态价格通道中评估其趋势。
当趋势得分为正时做多，负时做空。

## 细节

- **入场**：
  - **多头**：趋势得分 > 0
  - **空头**：趋势得分 < 0
- **出场**：反向信号
- **指标**：SMA、最高值、最低值
- **周期**：可配置
- **类型**：趋势跟随
