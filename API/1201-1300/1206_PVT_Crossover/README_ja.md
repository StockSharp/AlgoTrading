# PVTクロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はPrice Volume Trend (PVT)インジケーターとその指数移動平均線(EMA)のクロスオーバーに基づいて取引します。PVTがEMAを上抜けたときにロングポジション、下抜けたときにショートポジションを開きます。

## 詳細

- **エントリー条件**:
  - **ロング**: PVTがEMAを上抜ける。
  - **ショート**: PVTがEMAを下抜ける。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 反対のシグナルでポジションを反転。
- **ストップ**: なし。
- **デフォルト値**:
  - `EmaLength` = 20.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: PVT, EMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
