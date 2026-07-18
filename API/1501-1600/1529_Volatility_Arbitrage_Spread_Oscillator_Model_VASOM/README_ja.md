# ボラティリティ裁定スプレッドオシレーターモデル (VASOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

第1限月と第2限月の契約間のスプレッドのRSIが閾値を下回ったときに、VIXの当限月先物をロングします。RSIが出口レベルを上回ったときにポジションをクローズします。

## 詳細
- **エントリー条件**: スプレッドRSI < `LongThreshold`.
- **ロング/ショート**: ロングのみ.
- **エグジット条件**: スプレッドRSI > `ExitThreshold`.
- **ストップ**: なし.
- **デフォルト値**:
  - `RsiPeriod` = 2
  - `LongThreshold` = 46
  - `ExitThreshold` = 76
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `SecondSecurity` = "CBOE:VX2!"
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: ロングのみ
  - インジケーター: RSI
  - ストップ: なし
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
