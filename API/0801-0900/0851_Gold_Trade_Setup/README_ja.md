# ゴールド・トレードセットアップ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

カウフマン適応移動平均とSuperTrendに基づく戦略。
AMAが上昇しSuperTrendが上昇トレンドに切り替わったときに売ります。
AMAが下降しSuperTrendが下降トレンドに切り替わったときに買います。

## 詳細

- **エントリー条件**: AMAの方向とSuperTrendの切り替え。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 固定の目標とストップレベル。
- **ストップ**: はい。
- **デフォルト値**:
  - `AmaLength` = 14
  - `FastLength` = 2
  - `SlowLength` = 30
  - `AtrPeriod` = 10
  - `Factor` = 3.0
  - `TargetMultiplier` = 3.0
  - `RiskMultiplier` = 1.0
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: KAMA, SuperTrend
  - ストップ: はい
  - 複雑さ: 中
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
