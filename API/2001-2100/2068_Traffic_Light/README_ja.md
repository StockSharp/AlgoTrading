# 信号機戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

信号機のように色付けされた移動平均のセットを使用してトレード方向を決定するトレンドフォローアプローチです。
戦略は価格が所定のゾーン内にあるのを待ち、市場に参入する前に平均の順序を確認します。

## 詳細

- **エントリーゾーン**:
  - デフォルト: 価格は赤（遅い）SMA と黄（中間）SMA の間にある必要があります。
  - `UseBlueRange` が有効な場合: 価格は青い EMA チャンネルの高値ラインと安値ラインの間にある必要があります。
- **エントリー条件**:
  - ロング: `green EMA > blueHigh EMA > yellow SMA > red SMA` かつ `price > green EMA`。
  - ショート: `green EMA < blueLow EMA < yellow SMA < red SMA` かつ `price < green EMA`。
- **エグジット条件**:
  - オプション: `CloseOnCross` が有効な場合、緑の EMA が黄の SMA を反対方向にクロスしたときポジションをクローズします。
- **ストップ**: 価格ステップで測定されるオプションのテイクプロフィットとストップロス。
- **ロング/ショート**: 両方。
- **デフォルト値**:
  - `RedMaPeriod` = 120
  - `YellowMaPeriod` = 55
  - `GreenMaPeriod` = 5
  - `BlueMaPeriod` = 24
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `TakeProfitTicks` = 120
  - `StopLossTicks` = 60
  - `UseBlueRange` = false
  - `CloseOnCross` = true
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: 移動平均
  - ストップ: オプション
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中程度
