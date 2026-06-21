# 移動平均クロスオーバー・スイング戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

高速EMAが中速EMAをクロスしたときに取引し、低速MAとMACDヒストグラムによるオプション確認を使用します。ATRベースのストップロスとテイクプロフィットを使用し、セカンダリMAクロスで退出することもできます。

## 詳細

- **エントリー条件**:
  - 高速EMAが中速EMAを上抜けでロング、下抜けでショート。
  - オプション: 価格が低速EMAの上/下にある。
  - オプション: MACDヒストグラムがゼロより上/下にある。
- **ロング/ショート**: 設定可能。
- **エグジット条件**: ATRベースのストップロスとテイクプロフィット、またはオプションの退出MAクロス。
- **ストップ**: あり、ATRの倍数。
- **デフォルト値**:
  - `FastPeriod` = 5
  - `MediumPeriod` = 10
  - `SlowPeriod` = 50
  - `FastExitPeriod` = 5
  - `MediumExitPeriod` = 10
  - `AtrPeriod` = 14
  - `AtrStopMultiplier` = 1.4
  - `AtrTakeMultiplier` = 3.2
  - `EnableSlow` = true
  - `EnableMacd` = true
  - `EnableLong` = true
  - `EnableShort` = false
  - `EnableCrossExit` = true
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 設定可能
  - インジケーター: EMA, MACD, ATR
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: 1m (デフォルト)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
