# 調整可能なRSIとストップロス付きRSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSIが閾値を下回ったときに買いを入れ、価格が前のローソク足の高値を上抜けしたときにロングポジションをクローズします。各取引はパーセンテージベースのストップロスで保護されます。

## 詳細

- **エントリー条件**:
  - ロング: RSIが`RsiThreshold`を下回る
- **ロング/ショート**: ロング
- **エグジット条件**:
  - 終値が前のローソク足の高値を上回る
  - ストップロス
- **ストップ**: はい
- **デフォルト値**:
  - `RsiLength` = 8
  - `RsiThreshold` = 28m
  - `StopLossPercent` = 5m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: ロング
  - インジケーター: RSI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: なし
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
