# Flex ATR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Flex ATRは現在の時間軸に基づいてEMA、RSI、ATRの期間を動的に選択します。高速EMAが低速EMAを上抜けしRSIが50を超えるとロングエントリーします。逆のクロスオーバーでRSIが50を下回るとショートエントリーします。エグジットはATRベースのストップ・ターゲット、またはオプションのトレーリングストップを使用します。

## 詳細

- **エントリー条件**: 高速EMAと低速EMAのクロス + RSIフィルター。
- **ロング/ショート**: 両方。
- **エグジット条件**: ATRベースのストップまたはターゲット、オプションのトレーリングストップ。
- **ストップ**: あり。
- **デフォルト値**:
  - `AtrStopMult` = 3
  - `AtrProfitMult` = 1.5
  - `EnableTrailingStop` = true
  - `AtrTrailMult` = 1
  - `CandleType` = TimeSpan.FromMinutes(30)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, RSI, ATR
  - ストップ: あり
  - 複雑さ: 上級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
