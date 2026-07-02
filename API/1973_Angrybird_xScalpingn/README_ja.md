# Angrybird xScalpingn 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Angrybird xScalpingn はマーチンゲール方式のスキャルピング戦略です。短期価格方向とRSIフィルターに基づいて最初のトレードを開きます。直近レンジから導出された動的ステップ分だけ価格がオープンポジションに反して動くと、戦略はボリュームをファクターで掛けた別のトレードを追加します。CCIが強い逆方向の動きを示したとき、またはストップロスやテイクプロフィットに達したとき、すべてのポジションがクローズされます。

## 詳細

- **エントリー条件**: 最初のトレードはRSIフィルターを伴って直近の終値方向に従います。価格が計算されたステップ分ポジションに逆行したときに追加トレードが開かれます。
- **ロング/ショート**: 両方向。
- **エグジット条件**: CCIのリバーサルまたは保護的なストップロス/テイクプロフィット。
- **ストップ**: はい。
- **デフォルト値**:
  - `Volume` = 0.01
  - `LotExponent` = 2
  - `DynamicPips` = true
  - `DefaultPips` = 12
  - `Depth` = 24
  - `Del` = 3
  - `TakeProfit` = 20
  - `StopLoss` = 500
  - `Drop` = 500
  - `RsiMinimum` = 30
  - `RsiMaximum` = 70
  - `MaxTrades` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: Grid
  - 方向: 両方
  - インジケーター: RSI, CCI
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
