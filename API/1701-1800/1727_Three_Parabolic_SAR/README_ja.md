# 3本の Parabolic SAR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

3本の Parabolic SAR 戦略は、6時間足、3時間足、1時間足で計算された3つのParabolic SARインジケーターを使用します。上位の2つの時間軸が方向を確認し、1時間足のSARが反転したときに1時間足でトレードを開きます。

## 詳細

- **エントリー条件**:
  - ロング：6時間足のSARがクローズを下回り、3時間足のSARも下回る。ショート：上回る。
  - 1時間足ではSARが価格をクロス：上から下へロング、下から上へショート。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 1時間足のSARがポジションに逆らって動いたとき、または上位時間軸のSARのいずれかが反転したときにポジションをクローズ。
- **ストップ**: なし。
- **デフォルト値**:
  - `Acceleration` = 0.02
  - `MaxAcceleration` = 0.2
  - `HigherTimeframe` = TimeSpan.FromHours(6)
  - `MiddleTimeframe` = TimeSpan.FromHours(3)
  - `TradingTimeframe` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Parabolic SAR
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: マルチ時間軸
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
