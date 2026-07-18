# Triple EMA + QQE トレンドフォロー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

2本のTEMAラインとQQEフィルターを組み合わせたトレンドフォロー戦略。
価格が両方のTEMAラインを上回り、QQEが強気シグナルを出したときにロングポジションを開きます。
反対の条件でショートポジションを開きます。
ポイント単位のトレーリングストップでオープンポジションを保護します。

## 詳細

- **エントリー条件**: QQEクロスを伴うTEMAの整列。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはトレーリングストップ。
- **ストップ**: あり。
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238m
  - `Tema1Length` = 20
  - `Tema2Length` = 40
  - `StopLossPips` = 120
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: EMA, QQE
  - ストップ: トレーリング
  - 複雑さ: 中級
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
