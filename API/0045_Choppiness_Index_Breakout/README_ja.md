# Choppiness Index Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Choppiness Indexは市場がトレンド状態かレンジ状態かを計測します。インジケーターが閾値を下回ると、もみ合い環境からトレンドが始まることを示します。

テストでは年平均リターン約172%を示しています。外国為替市場での運用に最も適しています。

この戦略は、choppinessが低下した際に移動平均に対するPrice方向にエントリーします。choppinessが高い閾値を再び上回るか、ストップロスが発動した場合に決済します。

目的は、コンソリデーション期間から生まれる新しいトレンドを捉えることです。

## 詳細

- **エントリー条件**: Choppinessが`ChoppinessThreshold`を下回り、価格がMAの上/下にある。
- **ロング/ショート**: 両方向。
- **エグジット条件**: Choppinessが`HighChoppinessThreshold`を上回るまたはストップ。
- **ストップ**: あり。
- **デフォルト値**:
  - `MAPeriod` = 20
  - `ChoppinessPeriod` = 14
  - `ChoppinessThreshold` = 38.2m
  - `HighChoppinessThreshold` = 61.8m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: Choppiness, MA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

