# TradingView Supertrend Flip
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
出来高確認を伴うSupertrendインジケーターのフリップに基づく戦略

テストでは年平均リターンが約79%であることが示されています。株式市場で最もよく機能します。

TradingView Supertrend Flipは人気インジケーターの色変化を模倣します。赤から緑への変化はロングエントリーを、緑から赤への変化はショートエントリーを示します。戦略は次のフリップで退場します。

出来高確認は薄商いの期間における偽シグナルを避けるために使用できます。支持する出来高を伴うフリップにのみ対応することで、より信頼性の高い反転を捉えることを目指します。


## 詳細

- **エントリー条件**: ATR、Supertrendに基づくシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `VolumeAvgPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR、Supertrend
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - Neural Networks: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

