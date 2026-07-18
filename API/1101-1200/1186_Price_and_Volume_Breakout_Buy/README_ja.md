# 価格・出来高ブレイクアウト買い戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、価格がトレンドSMAの上に維持されながら、価格と出来高が同時にそれぞれのルックバック高値を上回ったときにエントリーします。ショート取引は、同じ出来高条件とSMAフィルターのもとで価格がルックバック安値を下回ったときに発動します。SMAの反対側で5本連続して引けた後にポジションが決済されます。

## 詳細
- **エントリー条件**:
  - **ロング**: Close > 前の最高値 && Volume > 前の最高出来高 && Close > SMA
  - **ショート**: Close < 前の最安値 && Volume > 前の最高出来高 && Close < SMA
- **ロング/ショート**: 設定可能
- **エグジット条件**:
  - **トレンド**: SMAを超えた5本連続クローズ
- **ストップ**: いいえ
- **デフォルト値**:
  - `PriceBreakoutPeriod` = 60
  - `VolumeBreakoutPeriod` = 60
  - `TrendlineLength` = 200
  - `OrderDirection` = "Long"
  - `CandleType` = TimeSpan.FromDays(1)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 設定可能
  - インジケーター: Highest, SMA, Volume
  - Stops: いいえ
  - 複雑さ: 基本
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
