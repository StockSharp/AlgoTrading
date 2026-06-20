# RSI Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI平均回帰に基づく戦略

テストでは年平均リターン約115%が示されています。株式市場で最もパフォーマンスが高くなります。

RSI Reversionは、価格が極端なRSI値に達した後に回帰すると仮定します。RSIが下限閾値を下回ると買い、上限閾値を上回ると売ります。ポジションはRSIが中立レベルに戻るにつれて決済されます。

極値は様々な市場に合わせて調整できます。トレンドの方向などの追加フィルターを使用することで、強い動きに早すぎる段階で逆張りすることを避けるのに役立ちます。


## 詳細

- **エントリー条件**: RSIに基づくシグナル。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `OversoldThreshold` = 30m
  - `OverboughtThreshold` = 70m
  - `ExitLevel` = 50m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

