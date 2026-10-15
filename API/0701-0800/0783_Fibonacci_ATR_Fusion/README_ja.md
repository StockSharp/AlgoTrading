# Fibonacci ATR Fusion戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

複数のFibonacci期間にわたる買い圧力比率とATRを組み合わせ、閾値クロスでエントリーとエグジットを行います。オプションのATRベース段階的テイクプロフィット付き。

## 詳細

- **エントリー条件**:
  - **ロング**: 加重平均が `LongEntryThreshold` を上抜け。
  - **ショート**: 加重平均が `ShortEntryThreshold` を下抜け。
- **エグジット条件**:
  - 加重平均が反対のエグジット閾値をクロス、またはポジション反転。
- **インジケーター**:
  - ATRに対する加重買い圧力比率。
  - オプションのテイクプロフィット用ATR。
- **ストップ**: なし。
- **デフォルト値**:
  - `LongEntryThreshold` = 58
  - `ShortEntryThreshold` = 42
  - `LongExitThreshold` = 42
  - `ShortExitThreshold` = 58
  - `Tp1Atr` = 3
  - `Tp2Atr` = 8
  - `Tp3Atr` = 14
  - `Tp1Percent` = 12
  - `Tp2Percent` = 12
  - `Tp3Percent` = 12
- **フィルター**:
  - トレンドフォロー
  - 単一時間軸
  - インジケーター: ATR
  - ストップ: なし
  - 複雑さ: 中程度
