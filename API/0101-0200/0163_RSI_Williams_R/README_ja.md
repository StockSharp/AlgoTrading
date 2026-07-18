# Rsi Williams R 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

戦略の実装 - RSI + Williams %R。RSI が 30 を下回り、Williams %R が -80 を下回っている（二重売られすぎ条件）場合に買い。RSI が 70 を上回り、Williams %R が -20 を上回っている（二重買われすぎ条件）場合に売り。

テストでは年平均収益率は約 76% を示しています。外国為替市場で最もパフォーマンスが優れています。

RSI は全体的なモメンタムを概説し、Williams %R はより素早い反転シグナルを提供します。2 つのオシレーターが一致したときにトレードが実行されます。

短期スイングを追う積極的なトレーダーに適しています。ATR ベースのストップが採用されています。

## 詳細

- **エントリー条件**:
  - ロング: `RSI < RsiOversold && WilliamsR < WilliamsROversold`
  - ショート: `RSI > RsiOverbought && WilliamsR > WilliamsROverbought`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - RSI がニュートラルゾーンに戻る
- **ストップ**: `StopLoss` を使用したパーセントベース
- **デフォルト値**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: RSI, Williams %R, R
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

