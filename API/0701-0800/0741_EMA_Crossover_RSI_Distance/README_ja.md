# EMAクロスオーバー・RSI・距離戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、複数のEMAとRSIを使用してロングおよびショートシグナルを生成し、速いEMA間の距離をチェックしてトレンドの強さを確認します。

## 詳細

- **エントリー条件**:
  - EMA5がEMA13を上回っている。
  - EMA40がEMA55を上回っている。
  - RSIが50を上回り、そのSMAを上回っている。
  - EMA5とEMA13の距離がその平均を上回り、EMA40-EMA13の距離が拡大している。
  - 終値がEMA5を上回っている。
- **ロング/ショート**: ロングとショート。
- **エグジット条件**:
  - シグナルがニュートラルまたは逆方向に変化する。
- **ストップ**: なし。
- **デフォルト値**:
  - `EmaShortLength` = 5
  - `EmaMediumLength` = 13
  - `EmaLong1Length` = 40
  - `EmaLong2Length` = 55
  - `RsiLength` = 14
  - `RsiAverageLength` = 14
  - `DistanceLength` = 5
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: EMA、RSI
  - ストップ: いいえ
  - 複雑さ: 中
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
