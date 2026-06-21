# Grover Llorens Activator戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ATRベースの適応型トレーリング戦略で、価格が内部アクティベーターラインを越えると方向を切り替えます。

価格とトレーリングラインの差がゼロを上回るときに買い。ゼロを下回るときに売り。

## 詳細

- **エントリー条件**: ATRから計算されたトレーリングストップをPrice が超えたとき。
- **ロング/ショート**: 両方。
- **エグジット条件**: 反対のシグナル。
- **ストップ**: なし。
- **デフォルト値**:
  - `Length` = 480
  - `Multiplier` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ATR
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
