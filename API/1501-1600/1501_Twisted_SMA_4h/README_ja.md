# Twisted SMA 戦略 4h
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Twisted SMA戦略は4時間足ロウソク足で3本の単純移動平均線とKAMAフィルターを使用する。速い SMAが中間SMAより上、中間が遅いSMAより上、価格がより長いSMAより上にあり、かつKAMAが横ばいでない場合にロングポジションを建てる。SMAが弱気な配列になった時にポジションを決済する。

## 詳細

- **エントリー条件**: 速いSMA > 中間SMA > 遅いSMA、終値 > メインSMA、KAMAが横ばいでない。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 速いSMA < 中間SMA < 遅いSMA。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `FastLength` = 4
  - `MidLength` = 9
  - `SlowLength` = 18
  - `MainSmaLength` = 100
  - `KamaLength` = 25
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: SMA, KAMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
