# 反射EMA差分RED戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は2つのHull Moving Averageの距離を反射させ、平滑化された値を追跡します。平滑化された反射が指定されたパーセンテージだけ反転すると、それに応じてロングまたはショートポジションに入ります。

## 詳細

- **エントリー条件**:
  - ロング: 平滑化された反射がプルバック限界を上回る。
  - ショート: 平滑化された反射がプルバック限界を下回る。
- **ロング/ショート**: 両方
- **デフォルト値**:
  - `Smoothing Period` = 2
  - `Change Percent` = 0.04
