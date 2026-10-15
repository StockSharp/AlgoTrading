# ダイナミック・ティックス・オシレーター・モデル (DTOM)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Dynamic Ticks Oscillator Model** は NYSE Down Ticks 指数の変化率を使用します。ROC が標準偏差に基づくダイナミック閾値を下回ったとき、戦略はロングポジションを建てます。ROC が正の閾値を上回ったときにポジションを閉じます。

## 詳細
- **エントリー条件**: `ROC < -StdDev * EntryStdDevMultiplier`
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: `ROC > StdDev * ExitStdDevMultiplier`
- **ストップ**: いいえ。
- **デフォルト値**:
  - `RocLength = 5`
  - `VolatilityLookback = 24`
  - `EntryStdDevMultiplier = 1.6m`
  - `ExitStdDevMultiplier = 1.4m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: ロングのみ
  - インジケーター: RateOfChange, StandardDeviation
  - ストップ: いいえ
  - 複雑さ: 初心者
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
