# PercentXトレンドフォロー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

TrendoscopeによるPercentX Trend Followerを基にした戦略です。

この戦略は、選択したバンド（KeltnerまたはBollinger）からの価格距離を正規化し、このオシレーターが動的な極値レンジをクロスした際に取引します。ストップの配置にはATRを使用します。

## 詳細

- **エントリー条件**: オシレーターが上部レンジを上抜けでロング、下部レンジを下抜けでショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRベースのストップ。
- **ストップ**: 初期ATRストップ。
- **デフォルト値**:
  - `BandType` = Keltner
  - `MaLength` = 40
  - `LoopbackPeriod` = 80
  - `OuterLoopback` = 80
  - `UseInitialStop` = true
  - `AtrLength` = 14
  - `TrendMultiplier` = 1
  - `ReverseMultiplier` = 3
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: BollingerBands, KeltnerChannels, ATR, Highest, Lowest
  - ストップ: ATR
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
