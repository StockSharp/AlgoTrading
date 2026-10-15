# Charles ブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

日次の高値・安値レベルに基づくブレイクアウト戦略です。RSI と EMA のトレンドフィルターを使用して、前日のレンジを超える価格の動きを探します。この戦略は日次の高値と安値を計算し、設定可能なデルタでオフセットし、トレンド条件が確認されたときに上限レベルを超えたらロング、下限レベルを下回ったらショートにエントリーします。

## 詳細

- **エントリー条件**:
  - ロング: `Close > DailyHigh + Delta` かつ `RSI > 55` かつ `FastEMA > SlowEMA`
  - ショート: `Close < DailyLow - Delta` かつ `RSI < 45` かつ `FastEMA < SlowEMA`
- **ロング/ショート**: 両方
- **エグジット条件**: 反対のシグナルまたは保護
- **ストップ**: パーセントで設定可能なテイクプロフィットとストップロス
- **デフォルト値**:
  - `Delta` = 0.0002m
  - `FastPeriod` = 18
  - `SlowPeriod` = 60
  - `RsiPeriod` = 14
  - `TakeProfit` = 1m
  - `StopLoss` = 0.5m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: EMA, RSI
  - ストップ: あり
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
