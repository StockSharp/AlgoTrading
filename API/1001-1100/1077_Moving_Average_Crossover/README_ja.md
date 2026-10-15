# 移動平均クロスオーバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

短期SMAが長期SMAを上回るとき買い、下回るときに売ります。反対のシグナルでポジションが反転します。

## 詳細

- **エントリー条件**:
  - 短期SMAが長期SMAを上抜けたときロング。
  - 短期SMAが長期SMAを下抜けたときショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のクロスオーバーで反転。
- **ストップ**: なし。
- **デフォルト値**:
  - `ShortLength` = 9
  - `LongLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: Crossover
  - 方向: 両方
  - インジケーター: SMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
