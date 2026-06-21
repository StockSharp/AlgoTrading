# フォローライン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、オプションのATRオフセットを含むボリンジャーバンドのブレイクアウトから導出されたフォローラインを追跡します。ラインが方向を反転したときにエントリーし、オプションで上位時間軸のトレンドで確認します。

## 詳細

- **エントリー条件**: 価格がボリンジャーバンドをブレイクした後にフォローラインが方向を変え、オプションで上位時間軸の確認を使用。
- **ロング/ショート**: 両方。
- **エグジット条件**: フォローラインまたは上位時間軸のトレンドが反転。
- **ストップ**: なし。
- **デフォルト値**:
  - `AtrPeriod` = 5
  - `BbPeriod` = 21
  - `BbDeviation` = 1
  - `UseAtrFilter` = true
  - `UseTimeFilter` = false
  - `Session` = "0000-2400"
  - `UseHtfConfirmation` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HtfCandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Bollinger Bands, ATR
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
