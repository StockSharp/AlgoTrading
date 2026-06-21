# Collector v1.0戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、固定距離で区切られた動的な買いまたは売りレベルに価格が達したときに成行注文を建てます。指定された取引数の後、出来高が増加します。累計利益が閾値を超えるとすべてのポジションが決済されます。

## 詳細

- **エントリー条件**:
  - ロング: 終値 >= 買いレベル
  - ショート: 終値 <= 売りレベル
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 総利益 >= ProfitCloseのときにすべて決済
- **ストップ**: なし
- **デフォルト値**:
  - `Distance` = 10m
  - `InitialVolume` = 0.01m
  - `VolumeStep` = 0.01m
  - `IncreaseTrade` = 3
  - `MaxTrades` = 200
  - `ProfitClose` = 500000m
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: グリッド
  - 方向: 両方
  - インジケーター: なし
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 高
