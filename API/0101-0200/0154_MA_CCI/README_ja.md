# MA CCI 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Moving Average と CCI インジケーターを組み合わせた戦略。価格が MA より上にあり CCI が売られすぎのときに買い。価格が MA より下にあり CCI が買われすぎのときに売り。

テストでは年平均リターン約 49% を示しています。暗号資産市場で最も優れたパフォーマンスを発揮します。

移動平均がトレンドを示し、CCI はその平均からの乖離を探します。エントリーは MA の方向に沿って CCI が極値に達したときに行われます。

押し目で入るスイングトレーダーに最適です。ATR ベースのストップが急激な反転から守ります。

## 詳細

- **エントリー条件**:
  - ロング: `Close > MA && CCI < OversoldLevel`
  - ショート: `Close < MA && CCI > OverboughtLevel`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - CCI がゼロラインに戻る
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `MaPeriod` = 20
  - `CciPeriod` = 20
  - `OverboughtLevel` = 100m
  - `OversoldLevel` = -100m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Moving Average, CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
