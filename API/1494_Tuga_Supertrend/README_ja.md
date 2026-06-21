# Tuga Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Tuga SupertrendはSuperTrendインジケーターに基づくロング専用戦略。SuperTrendの方向が下向きに転換したときにロングポジションを建て、方向が上向きに転換したときに決済する。

## 詳細
- **データ**: 価格ローソク足。
- **エントリー条件**:
  - **ロング**: 日付ウィンドウ内でSuperTrendの方向が上向きから下向きに変化する。
- **エグジット条件**: SuperTrendの方向が下向きから上向きに変化する。
- **ストップ**: なし。
- **デフォルト値**:
  - `StartDate` = 2018-01-01
  - `EndDate` = 2069-12-31
  - `AtrPeriod` = 10
  - `Factor` = 3.0
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロングのみ
  - インジケーター: SuperTrend, ATR
  - 複雑さ: 低
  - リスクレベル: 中
