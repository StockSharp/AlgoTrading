# AnchoredMomentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

AnchoredMomentum戦略は、ローソク足の終値のEMAとSMAの比率を計算します。モメンタムが上限しきい値を超えるとロングポジションをオープンし、下限しきい値を下回るとショートポジションをオープンします。逆のシグナルが現在のポジションをクローズします。

## 詳細

- **エントリー条件**: モメンタムが`UpLevel`を上抜けでロング、`DownLevel`を下抜けでショート。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆のシグナルがポジションをクローズ。
- **ストップ**: なし。
- **デフォルト値**:
  - `SmaPeriod` = 8
  - `EmaPeriod` = 6
  - `UpLevel` = 0.025m
  - `DownLevel` = -0.025m
  - `CandleType` = TimeSpan.FromHours(4)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: SMA, EMA
  - ストップ: なし
  - 複雑さ: 基本
  - 時間軸: 4h
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
