# Turtle Trader V1戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Turtle Trader V1は複数のモメンタム・オシレーターと移動平均フィルターを組み合わせます。高速EMAが低速EMAを上回り、RSI、ストキャスティクス、CCI、モメンタム、チャイキン・オシレーターがすべて上向きのときにロングポジションを開きます。ショートには逆の条件が必要です。

## 詳細

- **エントリー条件**:
  - 高速EMAが低速EMAより上（ショートは逆）
  - ロング：RSIが上昇中で70未満、ショート：RSIが下降中で30超
  - ロング：ストキャスティクス%Kが88未満、ショート：12超
  - ロング：CCIとモメンタムが上昇、ショート：下降
  - チャイキン・オシレーターが取引方向に動いている
- **ロング/ショート**: 両方
- **エグジット条件**: 逆のシグナル
- **ストップ**: デフォルトはなし
- **デフォルト値**:
  - `FastMaPeriod` = 10
  - `SlowMaPeriod` = 50
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `CciPeriod` = 20
  - `MomentumPeriod` = 10
  - `ChoFastPeriod` = 3
  - `ChoSlowPeriod` = 10
- **フィルター**:
  - カテゴリ: トレンドフォロー / モメンタム
  - 方向: 両方
  - インジケーター: EMA, RSI, Stochastic, CCI, Momentum, Chaikin Oscillator
  - ストップ: なし
  - 複雑さ: 上級
  - 時間軸: 1時間
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
