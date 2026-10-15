# トリプルMA HTF戦略 - 動的スムージング
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

上位時間軸で計算した3本の移動平均線を比較する戦略。
各上位時間軸MAは、その時間軸と作業時間軸の比率に比例してスムージングされる。
第1MAが第2MAをクロスし、第3MAが方向を確認したときにシグナルが生成される。

## 詳細

- **エントリー条件**: MA3のトレンド確認を伴うMA1とMA2のクロス。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナル。
- **ストップ**: いいえ。
- **デフォルト値**:
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `HigherTimeFrame1` = TimeSpan.FromMinutes(15)
  - `HigherTimeFrame2` = TimeSpan.FromMinutes(60)
  - `HigherTimeFrame3` = TimeSpan.FromMinutes(240)
  - `Length1` = 21
  - `Length2` = 21
  - `Length3` = 50
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: MA
  - ストップ: なし
  - 複雑さ: 中級
  - 時間軸: イントラデイ (ベース 5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
