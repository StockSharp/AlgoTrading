# 注文サンプル戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MQL5サンプル `OrderExample.mq5` から変換されたブレイクアウト戦略です。
価格が直近の高値を上回るか、直近の安値を下回るときに取引を開始します。

この戦略は `Highest` と `Lowest` インジケーターを使用して、設定可能なウィンドウでブレイクアウトレベルを追跡します。

## 詳細

- **エントリー条件**:
  - ロング: `Close` が `Lookback` 本のローソク足の最高値を上回る
  - ショート: `Close` が `Lookback` 本のローソク足の最安値を下回る
- **ロング/ショート**: 両方
- **エグジット条件**: 反対方向のブレイクアウト
- **ストップ**: いいえ
- **デフォルト値**:
  - `Lookback` = 26
  - `CandleType` = `TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Highest, Lowest
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
