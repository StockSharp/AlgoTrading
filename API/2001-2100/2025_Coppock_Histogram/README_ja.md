# Coppockヒストグラム戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はCoppockヒストグラムの反転を取引します。インジケーターは2つのRate of Change値を合計し、移動平均で結果を平滑化します。モメンタムが上向きに転じると、戦略はロングポジションを開きショートを閉じます。下向きの転換はロングを閉じてショートに入ります。シグナルは確定済みローソク足のみで評価されます。

## 詳細

- **エントリー条件**: Coppockヒストグラムが上向き傾斜で買い、下向き傾斜で売り。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 反対のシグナルがオープンポジションを閉じる。
- **ストップ**: 明示的なストップロスやテイクプロフィットなし。
- **デフォルト値**:
  - `Roc1Period` = 14
  - `Roc2Period` = 11
  - `SmoothPeriod` = 3
  - `Volume` = 1m
  - `CandleType` = TimeSpan.FromHours(8)
- **フィルター**:
  - カテゴリ: オシレーター
  - 方向: 両方
  - インジケーター: RateOfChange, SimpleMovingAverage
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 8H
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
