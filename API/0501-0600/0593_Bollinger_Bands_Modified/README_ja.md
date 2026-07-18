# 改良版Bollinger Bands戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オプションのEMAトレンドフィルターを使用してBollinger Bandsのブレイクアウトを取引する戦略です。価格が上限バンドを上抜けたときにロング、下限バンドを下抜けたときにショートに入ります。

ストップロスは直近の高値または安値に置き、テイクプロフィットはリスクの倍数です。

## 詳細

- **エントリー条件**:
  - ロング: 価格がBollinger Bands上限を上抜け
  - ショート: 価格がBollinger Bands下限を下抜け
- **ロング/ショート**: 両方
- **エグジット条件**:
  - ロング: 直近安値でストップ、目標はリスク * ファクター
  - ショート: 直近高値でストップ、目標はリスク * ファクター
- **ストップ**: 直近N本のローソク足の最高値/最安値
- **デフォルト値**:
  - `BollingerLength` = 20
  - `BollingerDeviation` = 0.38m
  - `EmaLength` = 80
  - `HighestLength` = 7
  - `LowestLength` = 7
  - `TargetFactor` = 1.6m
  - `EmaTrend` = true
  - `CrossoverCheck` = false
  - `CrossunderCheck` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Bollinger Bands, EMA, Highest, Lowest
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
