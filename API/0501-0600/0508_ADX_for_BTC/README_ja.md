# BTC 向け ADX 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、Average Directional Index (ADX) とオプションの SMA トレンドフィルターを使用して、Bitcoin の強い動きを捉えます。

テストでは平均年間リターン約 80% が示されています。暗号資産市場で最もよく機能します。

ADX がエントリーレベルを上抜けし、トレンドフィルターが強気の場合に買い参入します。ADX がエグジットレベルを下抜けしたらポジションを閉じます。

## 詳細

- **エントリー条件**: ADX が `EntryLevel` を上抜け、かつ（有効の場合）高速 SMA > 低速 SMA。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: ADX が `ExitLevel` を下抜け。
- **ストップ**: なし。
- **デフォルト値**:
  - `EntryLevel` = 14m
  - `ExitLevel` = 45m
  - `SmaFilter` = true
  - `SmaLength` = 200
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロングのみ
  - インジケーター: ADX, SMA
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
