# EMA 34 クロスオーバーとブレークイーブン・ストップロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**EMA 34 Crossover with Break Even Stop Loss** 戦略は、価格が34期間EMAを上抜けしたときにロングポジションを取ります。ストップロスは前のローソク足の安値に置き、テイクプロフィットはリスクの10倍、価格がリスクの3倍に達した後はストップをブレークイーブンに移動させます。

## 詳細
- **エントリー条件**: 終値が EMA(34) を下から上にクロスする。
- **ロング/ショート**: ロングのみ。
- **エグジット条件**: 前の安値でのストップロスまたは10×リスクでのテイクプロフィット。
- **ストップ**: はい、ブレークイーブン・ストップ。
- **デフォルト値**:
  - `EmaPeriod = 34`
  - `TakeProfitMultiplier = 10m`
  - `BreakEvenMultiplier = 3m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: EMA
  - ストップ: はい
  - 複雑さ: 初心者
  - 時間軸: イントラデイ (5m)
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
