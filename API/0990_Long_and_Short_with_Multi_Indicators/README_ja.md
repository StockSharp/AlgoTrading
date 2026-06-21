# 複数インジケーターを使ったロング・ショート戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はRSI、Rate of Change、および選択可能な移動平均を使用してロングとショートのシグナルを生成します。エグジットにはATRベースのトレーリングストップを適用します。

## 詳細

- **エントリー条件**:
  - ロング: RSIが売られすぎと買われすぎの間、ROC > 0、価格がMAの上。
  - ショート: 弱気トレンドの確認、ROC < 0、価格がMAの下。
- **ロング/ショート**: ロングとショート。
- **エグジット条件**:
  - ATRベースのトレーリングストップまたはインジケーターのストップ条件。
- **ストップ**: ATRトレーリングストップ。
- **デフォルト値**:
  - `RsiLength` = 5
  - `RsiOverbought` = 70
  - `RsiOversold` = 44
  - `RocLength` = 4
  - `MaLength` = 24
  - `MaTypeParam` = TEMA
  - `AtrLength` = 14
  - `AtrMultiplier` = 2
  - `BearishMaLength` = 200
  - `BearishTrendDuration` = 5
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング & ショート
  - インジケーター: RSI, ROC, MA, ATR
  - ストップ: はい
  - 複雑さ: 中
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
