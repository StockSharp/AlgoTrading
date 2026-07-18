# Digital CCI Woodies戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は2つの商品チャネル指数（CCI）インジケーターのクロスオーバーで取引します。高速CCIは価格変動に素早く反応し、低速CCIは市場ノイズを平滑化します。高速ラインが低速ラインを交差したときにシグナルが生成されます。

## 詳細

- **エントリー条件**:
  - ロング：高速CCIが低速CCIを上抜ける。
  - ショート：高速CCIが低速CCIを下抜ける。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - 高速CCIが低速CCIを下抜けたとき、ロングポジションを決済。
  - 高速CCIが低速CCIを上抜けたとき、ショートポジションを決済。
- **ストップ**: なし。
- **デフォルト値**:
  - `CandleType` = 6時間ローソク足
  - `FastLength` = 14
  - `SlowLength` = 6
  - `BuyOpen` = true
  - `SellOpen` = true
  - `BuyClose` = true
  - `SellClose` = true
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: CCI
  - ストップ: なし
  - 複雑さ: 低
  - 時間軸: 任意
  - 季節性: なし
  - ニューラルネットワーク: なし
  - ダイバージェンス: なし
  - リスクレベル: 中
