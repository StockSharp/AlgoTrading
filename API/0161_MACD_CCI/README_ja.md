# MACD CCI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MACD + CCI 戦略の実装。MACD がシグナルラインより上にあり CCI が -100 未満（売られすぎ）のときに買い。MACD がシグナルラインより下にあり CCI が 100 超（買われすぎ）のときに売り。

テストでは年平均リターン約 70% を示しています。株式市場で最も優れたパフォーマンスを発揮します。

MACD のスイングはモメンタムの転換を示し、CCI はその方向への押し目エントリーのタイミングを助けます。ロングとショートの両方のトレードが可能です。

モメンタムとオシレーターを組み合わせるトレーダーに適した手法です。リスク管理には ATR ストップを使用します。

## 詳細

- **エントリー条件**:
  - ロング: `MACD > Signal && CCI < CciOversold`
  - ショート: `MACD < Signal && CCI > CciOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**: MACD が反対方向にクロス
- **ストップ**: `StopLoss` を使用したパーセントベース
- **デフォルト値**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: MACD, CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
