# ICT Bread and Butter Sell-Setup 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ロンドン、ニューヨーク、アジアセッションの高値と安値を追跡し、それらを基にした事前定義されたセットアップで取引します。

## 詳細

- **エントリー条件**:
  - **NY ショート**: 価格がロンドンセッションより高い高値を付け、NYセッション中にローソク足が弱気で引ける。
  - **ロンドンクローズ 買い**: 10:30から13:00の間に価格がロンドンセッションの安値を下回って引ける。
  - **アジア ショート**: アジアセッション中に価格がアジアセッションの高値を上回って引ける。
- **ロング/ショート**: 両方向。
- **エグジット条件**:
  - 各取引はティック単位で定義されたストップロスとテイクプロフィットを使用する。
- **ストップ**: はい。
- **デフォルト値**:
  - `ShortStopTicks` = 10
  - `ShortTakeTicks` = 20
  - `BuyStopTicks` = 10
  - `BuyTakeTicks` = 20
  - `AsiaStopTicks` = 10
  - `AsiaTakeTicks` = 15
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **フィルター**:
  - カテゴリ: Price action
  - 方向: 両方
  - インジケーター: Price action
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
