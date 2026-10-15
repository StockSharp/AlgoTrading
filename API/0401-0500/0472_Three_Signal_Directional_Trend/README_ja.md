# Three Signal Directional Trend 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Three Signal Directional Trend 戦略は、MACD、ストキャスティクスオシレーター、移動平均の変化率を組み合わせてトレンドの方向性を判断します。各インジケーターがロングまたはショートの条件に投票し、少なくとも 2 つのインジケーターが一致したときにポジションを建てます。この手法は複数の確認シグナルを使ってノイズをフィルタリングしながら、広範な方向性のある動きを捉えることを目的としています。

## 詳細

- **エントリー条件:**
  - 3 つのシグナルのうち少なくとも 2 つが一致する。
  - **ロング**: MACD シグナルが上昇、ストキャスティクスが売られすぎ以下、MA ROC が正。
  - **ショート**: MACD シグナルが下降、ストキャスティクスが買われすぎ以上、MA ROC が負。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 反対方向のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `AvgLength` = 50
  - `RocLength` = 1
  - `AvgRocLength` = 10
  - `StochLength` = 14
  - `SmoothK` = 3
  - `Overbought` = 80
  - `Oversold` = 20
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdAvgLength` = 9
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: MACD、Stochastic、SMA、ROC
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
