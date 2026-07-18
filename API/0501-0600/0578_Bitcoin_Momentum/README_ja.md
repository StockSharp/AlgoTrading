# Bitcoin モメンタム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

価格が上位時間軸のEMAを上回り、警戒条件がないときのみ取引するBitcoinのモメンタム戦略です。ATRベースのトレーリングストップが利益を保護します。

## 詳細

- **エントリー条件**: 価格が週足EMAを上回り、警戒条件がない。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 価格がトレーリングストップまたは週足EMAを下回る。
- **ストップ**: ATRベースのトレーリングストップ。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromDays(1)
  - `HigherCandleType` = TimeSpan.FromDays(7)
  - `EmaLength` = 20
  - `AtrLength` = 5
  - `TrailStopLookback` = 7
  - `TrailStopMultiplier` = 0.2m
  - `StartTime` = 2000-01-01
  - `EndTime` = 2099-01-01
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロング
  - インジケーター: EMA, ATR, Highest
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
