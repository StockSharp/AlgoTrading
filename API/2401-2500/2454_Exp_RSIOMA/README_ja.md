# Exp RSIOMA戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Exp RSIOMA戦略は、RSI of Moving Average（RSIOMA）インジケーターを使用してトレンド反転とブレイクアウトを取引します。RSIの値は追加の移動平均によって平滑化され、シグナルラインとヒストグラムを形成します。この戦略は4つのモードをサポートします：

1. **Breakdown** – RSIが設定された高値/安値レベルをクロスしたときに取引。
2. **HistTwist** – ヒストグラムが方向を変えたときに取引。
3. **SignalTwist** – シグナルラインが方向を変えたときに取引。
4. **HistDisposition** – ヒストグラムがシグナルラインをクロスしたときに取引。

ポジションはロングとショートで独立して開閉できます。

## 詳細

- **エントリー条件**: `Mode`に依存
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `CandleType` = 4 hour
  - `RsiPeriod` = 14
  - `SignalPeriod` = 21
  - `HighLevel` = 20
  - `LowLevel` = -20
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: RSI
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
