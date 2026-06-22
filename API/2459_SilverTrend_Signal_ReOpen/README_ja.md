# SilverTrend Signal ReOpen戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

SilverTrendインジケーターに基づくオプションの再エントリー付き戦略です。インジケーターが方向を変えたときにポジションを開き、取引に有利な方向にあらかじめ定義されたステップ分価格が動くたびに追加ポジションを加えます。ポジションは逆シグナル時またはストップロス/テイクプロフィットレベルに達したときに決済できます。

## 詳細

- **エントリー条件**:
  - ロング: SilverTrendインジケーターが下降トレンドから上昇トレンドに転換
  - ショート: SilverTrendインジケーターが上昇トレンドから下降トレンドに転換
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 逆方向のSilverTrendシグナルで任意決済
  - ストップロスまたはテイクプロフィット到達
- **ストップ**: 絶対価格レベル
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `Ssp` = 9
  - `Risk` = 3
  - `PriceStep` = 300m
  - `PosTotal` = 10
  - `StopLoss` = 1000m
  - `TakeProfit` = 2000m
  - `BuyPosOpen` = true
  - `SellPosOpen` = true
  - `BuyPosClose` = true
  - `SellPosClose` = true
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SilverTrend
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
