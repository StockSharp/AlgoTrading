# アダプティブ HMA プラス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ボラティリティまたは出来高に基づいて期間を調整するアダプティブ Hull 移動平均戦略です。市場が活発な状況で HMA の傾きがトレンド方向を示しているときにロングまたはショートポジションを建てます。

## 詳細

- **エントリー条件**: アダプティブ HMA、ATR または出来高に基づくシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `MinPeriod` = 172
  - `MaxPeriod` = 233
  - `AdaptPercent` = 0.031m
  - `FlatThreshold` = 0m
  - `UseVolume` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MA, ATR, Volume
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

