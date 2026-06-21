# Trend Vanguard 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Trend Vanguard は高値と安値のシンプルな ZigZag を使用してトレンドの反転に追従します。
ZigZag が方向を変えると売買方向が切り替わります。

## 詳細

- **エントリー条件**: ZigZag の反転
- **ロング/ショート**: 両方
- **エグジット条件**: 逆の ZigZag シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `Depth` = 21
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Highest, Lowest
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
