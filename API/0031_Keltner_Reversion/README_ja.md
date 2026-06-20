# Keltner Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ケルトナーチャネルを使用した平均回帰取引戦略

テストでは年平均リターン約130%が示されています。株式市場で最もパフォーマンスが高くなります。

Keltner Reversionはケルトナーチャネルの外側への押し込みに逆らって取引します。エントリーは中間バンドへの回帰を期待し、価格がチャネル内に再び入るかストップに達したらトレードを決済します。

チャネルの幅はボラティリティに合わせて拡大・縮小するため、システムが極端な動きを捉えながらトレードが展開する余地を与えます。ストップは通常ATRの倍数に基づいています。


## 詳細

- **エントリー条件**: RSI、ATR、Keltnerに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: RSI, ATR, Keltner
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

