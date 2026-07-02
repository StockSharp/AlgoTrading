# Elder Impulse 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

エルダーのインパルスシステムに基づく戦略

テストでは年平均リターン約106%が示されています。株式市場で最もパフォーマンスが高くなります。

Elder ImpulseはEMAの方向とMACDヒストグラムの色を組み合わせています。EMAを上回る緑のバーはロングを促し、EMAを下回る赤のバーはショートを促し、ニュートラルなバーはエグジットを示します。

トレンドの方向とモメンタムを融合させることで、このアプローチはトレーダーを強い動きの正しい側に保ちます。エグジットはシンプルで、ヒストグラムの色の変化またはEMAの傾きの反転に依存します。


## 詳細

- **エントリー条件**: MACDに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `EmaPeriod` = 13
  - `MacdFastPeriod` = 12
  - `MacdSlowPeriod` = 26
  - `MacdSignalPeriod` = 9
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

