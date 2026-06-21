# Tomas比率戦略（マルチ時間軸分析付き）
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は複数の時間軸にわたって加重損益を蓄積し、Tomas Ratioシグナルを構築します。シグナル強度が目標を超えるとトレードを開き、弱さが支配的になると閉じます。

## 詳細

- **エントリー条件**: シグナル強度が目標を超え、価格がEMA(720)より上にあるとき
- **ロング/ショート**: ロングのみ
- **エグジット条件**: 終値ポイントが買いポイントを超えるとき
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = 1時間足ローソク
  - `Length` = 720
  - `DeviationLength` = 168
  - `PointsTarget` = 100
  - `UseStandardDeviation` = true
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロングのみ
  - インジケーター: Standard Deviation, SMA, EMA
  - ストップ: なし
  - 複雑さ: 上級
  - 時間軸: 複数
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
