# RSI + MACD ロングのみ戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

RSI が中心線を上抜けて MACD の強気確認が得られたとき、または RSI が中心線を上回りながら MACD がシグナル線を上抜けたときにロングエントリーします。RSI が中心線を下抜けるか、MACD がシグナル線を下抜けてヒストグラムが 0 以下になったときにエグジットします。オプションの EMA トレンドフィルターと売られすぎのコンテキストでエントリーを絞り込むことができます。

## 詳細

- **エントリー条件**: MACD の強気確認を伴い RSI が中心線を上抜けるか、RSI が中心線を上回りながら MACD がシグナル線を上抜ける
- **ロング/ショート**: ロングのみ
- **エグジット条件**: RSI が中心線を下抜けるか、ヒストグラム ≤ 0 で MACD がシグナル線を下抜ける
- **ストップ**: 任意のパーセント建てテイクプロフィットとストップロス
- **デフォルト値**:
  - `RsiLength` = 14
  - `RsiOversold` = 30
  - `RsiMidline` = 50
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `OversoldWindowBars` = 10
  - `EmaLength` = 200
  - `TakeProfitPercent` = 11.5
  - `StopLossPercent` = 2.5
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: RSI, MACD, EMA
  - ストップ: はい（オプション）
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
