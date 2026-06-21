# 強気リバーサルバー戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

強気リバーサルバー戦略の実装です。アリゲーターラインの下で強気リバーサルバーが形成され、価格がバーの高値を上抜けたときにロングエントリーします。オプションフィルターでAwesome OscillatorとMarket Facilitation Indexのスクワットバーを有効にできます。

このセットアップは、トレンドが強気に転換する中でキャンドルの上半分にクローズする新安値を探します。確認はバーの高値を価格が超えたときに得られます。

## 詳細

- **エントリー条件**:
  - ロング: `bullish reversal bar && close > confirmation level`
- **ロング/ショート**: ロングのみ
- **エグジット条件**:
  - バー安値でのストップロス、またはトレンドが下向きに転換
- **ストップ**: `_stopLoss`に格納されたバー安値
- **デフォルト値**:
  - `EnableAo` = false
  - `EnableMfi` = false
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: ロング
  - インジケーター: Alligator, Awesome Oscillator, Market Facilitation Index
  - ストップ: はい
  - 複雑さ: 上級
  - 時間軸: 短期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
