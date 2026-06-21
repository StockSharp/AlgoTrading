# Nifty 50 5分足戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Nifty 50 5分足戦略**は、DEMA、VWAP、ボリンジャーバンドの確認を使用してNifty 50指数のブレイクアウトを取引します。

## 詳細
- **エントリー条件**:
  - **ロング**: 終値が前回高値を上抜け、ボリンジャーバンド上限を上抜け、かつDEMAがVWAPを上回る。
  - **ショート**: 終値が前回安値を下抜け、ボリンジャーバンド下限を下抜け、かつDEMAがVWAPを下回る。
- **ロング/ショート**: 両方。
- **エグジット条件**: ストップロス。
- **ストップ**: あり、固定ポイント。
- **デフォルト値**:
  - `DemaPeriod = 6`
  - `BollingerLength = 20`
  - `BollingerStdDev = 2`
  - `LookbackPeriod = 5`
  - `StopLossPoints = 25`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: 両方
  - インジケーター: DEMA, VWAP, Bollinger Bands
  - ストップ: あり
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
