# Larry Connors ボリンジャー %B 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は Larry Connors の %B アプローチに従います。価格が 200 期間 SMA を上回る上昇トレンドにあり、Bollinger %B 値が 3 本連続のローソク足でしきい値を下回る場合に買います。%B が上方しきい値を上回ると、ポジションを決済します。

デフォルト設定は日足ローソク足を対象としています。

## 詳細

- **エントリー条件**: SMA200 を上回るクローズ、かつ 3 本連続のローソク足で %B が `LowPercentB` を下回ること。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: %B が `HighPercentB` を上抜けるか、ストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `SmaPeriod` = 200
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `LowPercentB` = 0.2m
  - `HighPercentB` = 0.8m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: Bollinger Bands, SMA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 日足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
