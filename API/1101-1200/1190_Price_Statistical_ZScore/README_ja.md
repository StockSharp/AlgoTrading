# 価格統計ZScore戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

平滑化されたZ-Scoreのクロスとローソク足モメンタムフィルターを使用した戦略。

短期Z-Scoreが長期Z-Scoreを上回ると買い、下回ると決済します。同じシグナルが複数回連続した後はシグナルを無視し、3本の上昇ローソク足の後のエントリーを避けます。

## 詳細

- **エントリー条件**: 短期Z-Scoreが長期を上回る、直前に3本の上昇バー連続がない、シグナル間に間隔がある。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 短期Z-Scoreが長期を下回る、直前に3本の下降バー連続がない、シグナル間に間隔がある。
- **ストップ**: なし。
- **デフォルト値**:
  - `ZScoreBasePeriod` = 3
  - `ShortSmoothPeriod` = 3
  - `LongSmoothPeriod` = 5
  - `GapBars` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: SMA, StandardDeviation
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
