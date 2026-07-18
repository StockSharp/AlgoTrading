# AntiFragile EA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

現在の価格の上下に増加するボリュームで層状の指値注文を配置するグリッド戦略です。
ポジションは初期ストップで保護され、価格が有利な方向に動くにつれてトレーリングされます。

## 詳細

- **エントリー条件**:
  - ロング: bid より`SpaceBetweenTrades`ステップ下方ごとにbuy limit注文を配置。
  - ショート: ask より`SpaceBetweenTrades`ステップ上方ごとにsell limit注文を配置。
- **ロング/ショート**: `TradeLong`と`TradeShort`でそれぞれオプション設定可能。
- **エグジット条件**: トレーリングストップまたは反対側のグリッド執行。
- **ストップ**: 初期`StopLossPips`および`TrailingStopPips`によるトレーリング。
- **デフォルト値**:
  - `StartingVolume` = 0.1m
  - `IncreasePercentage` = 1m
  - `SpaceBetweenTrades` = 700m
  - `NumberOfTrades` = 50
  - `StopLossPips` = 300m
  - `TrailingStopPips` = 100m
  - `TradeLong` = true
  - `TradeShort` = true
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: グリッド取引
  - 方向: 両方
  - インジケーター: なし
  - ストップ: トレーリング
  - 複雑さ: 中級
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
