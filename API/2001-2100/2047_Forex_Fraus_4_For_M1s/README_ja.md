# Forex Fraus 4 For M1s戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MQL4戦略#13643の変換版です。元のエキスパートアドバイザーはWilliams %Rインジケーターが極端なレベルに触れて戻ってくるときに取引を行います。このC#版はStockSharpの高レベルAPIを使用しています。

この戦略は1分足ローソク足で動作し、2つのキーレベルに反応します。
- Williams %Rがそれ以下にあった後-99.9を上回ったときにロングシグナルが生成されます。
- Williams %Rがそれ以上にあった後-0.1を下回ったときにショートシグナルが現れます。

ポジションは固定ストップ、ターゲット、またはトレーリングストップで決済されます。時間フィルターで取引を特定のイントラデイウィンドウに制限できます。

## 詳細

- **エントリー条件**  
  - ロング: `WilliamsR`が下方にあった後`BuyThreshold`(-99.9)を上抜け。  
  - ショート: `WilliamsR`が上方にあった後`SellThreshold`(-0.1)を下抜け。
- **ロング/ショート**: 両方
- **エグジット条件**  
  - 価格がストップロス(`StopLoss`)またはテイクプロフィット(`TakeProfit`)に達したとき  
  - 有効時にトレーリングストップ(`TrailingStop`)が起動
- **ストップ**: ステップベース
- **デフォルト値**  
  - `WprPeriod` = 360  
  - `BuyThreshold` = -99.9  
  - `SellThreshold` = -0.1  
  - `StopLoss` = 0  
  - `TakeProfit` = 0  
  - `UseProfitTrailing` = true  
  - `TrailingStop` = 30  
  - `TrailingStep` = 1  
  - `UseTimeFilter` = false  
  - `StartHour` = 7  
  - `StopHour` = 17  
  - `Volume` = 0.01  
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**  
  - カテゴリ: トレンドリバーサル  
  - 方向: 両方  
  - インジケーター: Williams %R  
  - ストップ: あり  
  - 複雑さ: 基本  
  - 時間軸: イントラデイ (M1)  
  - 季節性: いいえ  
  - ニューラルネットワーク: いいえ  
  - ダイバージェンス: いいえ  
  - リスクレベル: 中
