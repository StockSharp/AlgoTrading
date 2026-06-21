# My Line Order
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は価格が事前に定義した水平レベルをクロスした時に成行注文を発動します。ユーザーはロングとショートのエントリーに別々のレベルとpips単位のリスクパラメーターを指定します。ポジションを建てた後、戦略はストップロス、テイクプロフィット、オプションのトレーリングストップを追跡します。

エントリーレベルが事前にわかっている裁量トレードのセットアップに適しています。価格レベルのみに依存するため、あらゆる銘柄と時間軸で機能します。

## 詳細

- **エントリー条件**:
  - **ロング**: 終値が`BuyPrice`を上抜けする。
  - **ショート**: 終値が`SellPrice`を下抜けする。
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - `StopLossPips`でのストップロス。
  - `TakeProfitPips`でのテイクプロフィット。
  - `TrailingStopPips` > 0の場合はトレーリングストップ。
- **ストップ**: あり、pips単位。
- **デフォルト値**:
  - `BuyPrice` = 0 (無効)
  - `SellPrice` = 0 (無効)
  - `TakeProfitPips` = 30
  - `StopLossPips` = 20
  - `TrailingStopPips` = 0
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: 手動
  - 方向: 両方
  - インジケーター: なし
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
