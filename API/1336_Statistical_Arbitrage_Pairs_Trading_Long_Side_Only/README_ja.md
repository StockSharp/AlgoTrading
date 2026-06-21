# 統計的裁定ペアトレード戦略 - ロングサイドのみ
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、2つの銘柄間のスプレッドのZ-Scoreに基づくシンプルなペアトレードアプローチを実行します。スプレッドがユーザー定義の閾値を下回ったときにロングポジションを開き、スプレッドがゼロを上抜けたときにポジションを閉じます。

## 詳細

- **エントリー条件**: スプレッドのZ-Scoreが閾値を下回る。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: スプレッドのZ-Scoreがゼロを上抜ける。
- **ストップ**: なし。
- **デフォルト値**:
  - `ZScoreLength` = 20
  - `ExtremeLevel` = -1
  - `CandleType` = TimeSpan.FromMinutes(1)
- **フィルター**:
  - カテゴリ: Mean Reversion
  - 方向: ロングのみ
  - インジケーター: SMA, StandardDeviation
  - ストップ: なし
  - 複雑さ: 初心者
  - 時間軸: 任意
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
