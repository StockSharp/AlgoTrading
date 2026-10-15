# リバーサル取引ボット戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

リバーサル取引ボット戦略は、RSIダイバージェンスを使用し、オプションの出来高・ADX・ボリンジャーバンド・RSIクロスオーバーフィルターと組み合わせて市場の転換点を捉える。ポジションは固定パーセントのストップロスとテイクプロフィットで保護される。

## 詳細

- **エントリー条件**: RSIダイバージェンスとオプションの出来高・ADX・ボリンジャーバンド・RSIクロスオーバーフィルター
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロスまたはテイクプロフィット
- **ストップ**: 固定パーセント
- **デフォルト値**:
  - `RsiLength` = 8
  - `FastRsiLength` = 14
  - `SlowRsiLength` = 21
  - `BbLength` = 20
  - `AdxThreshold` = 20
  - `DivLookback` = 5
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 2
- **フィルター**:
  - カテゴリ: リバーサル
  - 方向: 両方
  - インジケーター: RSI, ADX, Bollinger Bands, SMA
  - ストップ: 固定
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: はい
  - リスクレベル: 中

