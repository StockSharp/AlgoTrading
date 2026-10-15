# Javo v1戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Javo v1はHeikin Ashiローソク足と一対の指数移動平均を組み合わせています。HAの方向と速い/遅いEMAのクロスオーバーが一致したときにポジションを建てます。このアプローチはノイズを平滑化しながら新興トレンドを捉えようとします。

## 詳細

- **エントリー条件**:
  - **ロング**: HA強気かつ `EMA_fast > EMA_slow`
  - **ショート**: HA弱気かつ `EMA_fast < EMA_slow`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆シグナル
- **ストップ**: なし
- **デフォルト値**:
  - `FastEmaPeriod` = 1
  - `SlowEmaPeriod` = 30
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Heikin Ashi, EMA
  - ストップ: いいえ
  - 複雑さ: 低
  - 時間軸: 1時間足
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
