# VIXスパイク戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

VIX指数が移動平均を標準偏差の倍数分上回ったときに買い、固定数のバー後にポジションをクローズします。

## 詳細

- **エントリー条件**: VIX > 平均 + StdDevMultiplier * 標準偏差.
- **ロング/ショート**: ロングのみ.
- **エグジット条件**: `ExitPeriods`バー後に決済.
- **ストップ**: はい.
- **デフォルト値**:
  - `StdDevLength` = 15
  - `StdDevMultiplier` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VixSecurity` = "CBOE:VIX"
- **フィルター**:
  - カテゴリ: ボラティリティ
  - 方向: ロングのみ
  - インジケーター: SMA, StdDev
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
