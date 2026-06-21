# PEAD戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、EPSのポジティブサプライズとギャップアップの後に生じる決算発表後ドリフト（PEAD）を取引します。
決算翌日に価格がギャップアップで始まり、直近のパフォーマンスが良好な場合にロングエントリーし、
EMAトレーリング、固定ストップ/ブレークイーブン、最大保有期間を使用します。

## 詳細

- **エントリー条件**: ポジティブなEPSサプライズ、決算後のギャップアップ、直近パフォーマンスが良好。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 日足EMAのクロスアンダー、固定ストップ/ブレークイーブン、または最大保有バー数。
- **ストップ**: ブレークイーブン付き固定ストップ。
- **デフォルト値**:
  - `GapThreshold` = 1
  - `EpsSurpriseThreshold` = 5
  - `PerfDays` = 20
  - `StopPct` = 8
  - `EmaLen` = 50
  - `MaxHoldBars` = 50
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: Earnings
  - 方向: Long
  - インジケーター: EMA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
