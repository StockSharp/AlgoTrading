# Mikul's Ichimoku Cloud v2 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

オプションの移動平均フィルターを備えたIchimoku Cloudを使用したブレイクアウト戦略。ポジションはトレーリングストップ（ATR、パーセント、またはIchimokuルール）とオプションのテイクプロフィットで管理されます。

## 詳細

- **エントリー条件**: 価格が雲の上にある状態でTenkan-senがKijun-senを上抜け、または緑の雲の上への強いブレイクアウト。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: トレーリングストップまたはIchimokuリバーサル、オプションのテイクプロフィット。
- **ストップ**: トレーリング。
- **デフォルト値**:
  - `TrailSource` = `LowsHighs`
  - `TrailMethod` = `Atr`
  - `TrailPercent` = 10
  - `SwingLookback` = 7
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 1
  - `AddIchiExit` = false
  - `UseTakeProfit` = false
  - `TakeProfitPercent` = 25
  - `UseMaFilter` = false
  - `MaType` = `Ema`
  - `MaLength` = 200
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouBPeriod` = 52
  - `Displacement` = 26
  - `CandleType` = TimeSpan.FromHours(1)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: ロング
  - インジケーター: Ichimoku、ATR
  - ストップ: トレーリング
  - 複雑さ: 中程度
  - 時間軸: イントラデイ (1h)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
