# Pivot Point SuperTrendトレンドフィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ピボットベースのSuperTrend線をSuperTrendトレンドフィルターおよび移動平均による確認と組み合わせます。トレンドが転換したとき、または指定した日付ウィンドウ内にピボットSuperTrendシグナルが現れたときに取引します。

## 詳細

- **エントリー条件**:
  - トレンドフィルターが上向きに転換し、価格が移動平均線の上にある。
  - Pivot SuperTrendが設定された日付範囲内で買いシグナルを発する。
- **エグジット条件**:
  - トレンドフィルターが下向きに転換するか、Pivot SuperTrendが売りシグナルを発する。
- **ストップ**: なし
- **デフォルト値**:
  - `PivotPeriod` = 2
  - `Factor` = 3
  - `AtrPeriod` = 10
  - `TrendAtrPeriod` = 10
  - `TrendMultiplier` = 3
  - `MaPeriod` = 20
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Pivot, SuperTrend, SMA
  - ストップ: いいえ
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: オプション
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
