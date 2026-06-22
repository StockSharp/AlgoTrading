# Q2MAクロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Q2MAクロス戦略は、ローソク足の終値と始値で構築された平滑化移動平均のクロスオーバーに基づいて取引します。終値の平均が始値の平均を上回った後に下回るとロングポジションを開き、逆のクロスオーバーではショートポジションを開きます。逆のトレンドが現れるとポジションをクローズします。この戦略はティック単位で測定されるストップロスとテイクプロフィットのレベルも適用します。

## 詳細

- **エントリー条件**: 終値と始値の移動平均のクロスオーバー
- **ロング/ショート**: 両方向
- **エグジット条件**: 逆クロスオーバーまたはストップロス/テイクプロフィット
- **ストップ**: あり
- **デフォルト値**:
  - `Length` = 8
  - `StopLoss` = 1000
  - `TakeProfit` = 2000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Volume` = 1
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
  - `Invert` = false
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Moving Average
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: H4
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
