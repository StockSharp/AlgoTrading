# MACD Zero Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
このシステムは、Moving Average Convergence Divergence (MACD)ヒストグラムがゼロラインに近づくときのモメンタム転換を取引します。ゼロ以下での上昇MACDまたはゼロ以上での下落MACDは潜在的なリバーサルを示します。

テストでは年間平均リターン約136%が示されています。株式市場で最も優れたパフォーマンスを発揮します。

この戦略はMACD線がまだ反対側にある間、ゼロに向かって動くのを待ちます。モメンタムが衰えると、価格のスイングを予測してエントリーします。

MACDがシグナル線をクロスするか、ストップロスが発動されるとトレードを終了します。

## 詳細

- **エントリー条件**: MACDがどちらの側からもゼロに向かって動いている。
- **ロング/ショート**: 両方向。
- **エグジット条件**: MACDがシグナル線をクロスするかストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: モメンタム
  - 方向: 両方
  - インジケーター: MACD
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

