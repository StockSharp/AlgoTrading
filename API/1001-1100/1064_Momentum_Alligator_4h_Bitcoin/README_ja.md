# モメンタム Alligator 4h Bitcoin戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

モメンタム Alligator 4h Bitcoin戦略は、日足時間軸でAwesome OscillatorとBill WilliamsのAlligatorを組み合わせます。オシレーターが5期間SMAを上抜けし、価格が日足Alligatorの3本の線すべてを上回って取引されているときにロングポジションを建てます。動的ストップロスはエントリーからのパーセント下落とAlligatorのジョー（下顎）ラインのうち大きい方を使用します。利益確定のエグジット後、戦略は次の2つのシグナルをスキップします。

## 詳細

- **エントリー条件**: AOが5期間SMAを上抜けし、終値が日足Alligatorの各ラインを上回っているとき。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: パーセントストップとAlligatorのジョーの最大値での動的ストップロス。
- **ストップ**: はい。
- **デフォルト値**:
  - `StopLossPercent` = 0.02m
  - `CandleType` = TimeSpan.FromHours(4)
  - `TradeStart` = 2023-01-01
  - `TradeStop` = 2025-01-01
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: ロングのみ
  - インジケーター: Awesome Oscillator、Alligator
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
