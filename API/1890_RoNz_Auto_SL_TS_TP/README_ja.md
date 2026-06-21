# RoNz Auto SL TS TP 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

EMAクロスオーバーでポジションをオープンし、ストップロスとテイクプロフィットのレベルを自動的に管理する戦略です。  
エントリー後、初期ストップとターゲットを設定し、その後オプションで利益をロックしてトレーリングストップを有効にします。

## 詳細

- **エントリー条件**:
  - ロング: `EMA10 < EMA20 && EMA10 > EMA100`
  - ショート: `EMA10 > EMA20 && EMA10 < EMA100`
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロス、テイクプロフィット、利益ロックまたはトレーリングストップ
- **ストップ**: はい
- **デフォルト値**:
  - `TakeProfit` = 500
  - `StopLoss` = 250
  - `LockProfitAfter` = 100
  - `ProfitLock` = 60
  - `TrailingStop` = 50
  - `TrailingStep` = 10
- **フィルター**:
  - カテゴリ: リスク管理
  - 方向: 両方
  - インジケーター: EMA
  - ストップ: SL/TP/Trailing
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
