# TF セグメント化線形回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

各時間セグメント内に線形回帰チャネルを適用する戦略です。価格が上限バンドを上抜けするとロングポジションを建て、下限バンドを下抜けするとショートポジションを建てます。

## 詳細
- **エントリー条件**: 価格が回帰チャネルを越える。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対側のバンドのクロスオーバー。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
  - `Segment` = TimeSpan.FromDays(1)
  - `Multiplier` = 2
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Linear Regression
  - ストップ: いいえ
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
