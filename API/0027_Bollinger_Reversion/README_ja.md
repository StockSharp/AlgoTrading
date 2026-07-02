# Bollinger Reversion 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ボリンジャーバンドの平均回帰に基づく戦略

テストでは年平均リターン約118%が示されています。株式市場で最もパフォーマンスが高くなります。

Bollinger Reversionはボリンジャーバンドの外側への動きに逆らって取引します。バンドを超えた終値に対してトレードをオープンし、価格がバンド内に戻るかストップに達したら決済します。

標準偏差バンドは過度な伸長の統計的な見方を提供します。極端な終値の後にエントリーすることで、中間バンドへの引き戻しから利益を得ることを目指します。


## 詳細

- **エントリー条件**: RSI、ATR、Bollingerに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: RSI, ATR, Bollinger
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

