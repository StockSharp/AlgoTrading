# デュアル SuperTrend VIX フィルター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、2 つの SuperTrend インジケーターと VIX ベースのボラティリティフィルターを組み合わせます。両方の SuperTrend が強気でかつ VIX インデックスがその平均を上回るときにロングポジションを建てます。両方の SuperTrend が弱気でかつ VIX が平均プラス標準偏差バッファーを上回って上昇しているときにショートポジションを建てます。どちらかの SuperTrend が方向転換したときにポジションをクローズします。

## 詳細

- **エントリー条件**:
  - **ロング**: 両方の SuperTrend が上昇トレンドを示し、VIX がその平均を上回っている。
  - **ショート**: 両方の SuperTrend が下降トレンドを示し、VIX がその平均を上回り上昇している。
- **エグジット条件**:
  - 逆方向の SuperTrend シグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `StLength1` = 13
  - `StMultiplier1` = 3.5
  - `StLength2` = 8
  - `StMultiplier2` = 5
  - `UseVixFilter` = true
  - `VixLookback` = 252
  - `VixTrendPeriod` = 10
  - `StdDevMultiplier` = 1
  - `EnableLong` = true
  - `EnableShort` = true
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: SuperTrend, SMA, StandardDeviation, EMA
  - ストップ: なし
  - 複雑さ: 中程度
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
