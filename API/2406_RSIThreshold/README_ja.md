# RSI閾値戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTraderの*Exp_RSI*エキスパートをStockSharpに変換します。相対力指数（RSI）が事前定義された買われすぎおよび売られすぎのレベルを越えるとき、戦略はポジションをオープンおよびクローズします。

## 詳細

- **エントリー条件**:
  - **ロング**: RSIが`RSI Low Level`を上回るとクロス。
  - **ショート**: RSIが`RSI High Level`を下回るとクロス。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 反対シグナルまたはストップパラメーター。
- **ストップ**: Take ProfitとStop Lossは絶対価格単位。
- **デフォルト値**:
  - `RSI Period` = 14
  - `RSI High Level` = 60
  - `RSI Low Level` = 40
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: 単一
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: H4
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
