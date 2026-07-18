# MACD CCI Lotfy戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

スケーリング係数を使用してMACDとCCIを組み合わせた戦略。
両インジケーターが同じ方向に極端なしきい値を交差した時にポジションを開きます。

MACD値に係数を掛けてCCIとのスケールを合わせ、同一しきい値での直接比較を可能にします。
このアプローチは買われ過ぎ・売られ過ぎゾーンからのリバーサルを捉えることを目的とします。

## 詳細

- **エントリー条件**:
  - ロング: `CCI < -Threshold` かつ `MACD * MacdCoefficient < -Threshold`
  - ショート: `CCI > Threshold` かつ `MACD * MacdCoefficient > Threshold`
- **ロング/ショート**: 両方
- **エグジット条件**: 逆方向のシグナルが逆ポジションを起動
- **ストップ**: なし
- **デフォルト値**:
  - `CciPeriod` = 8
  - `FastPeriod` = 13
  - `SlowPeriod` = 33
  - `MacdCoefficient` = 86000
  - `Threshold` = 85
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: MACD, CCI
  - ストップ: いいえ
  - 複雑さ: 基本
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
