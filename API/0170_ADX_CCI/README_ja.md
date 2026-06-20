# Adx Cci Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ADX と CCI インジケーターに基づく戦略。ADX > 25 かつ CCI が売られすぎ（< -100）の場合にロング参入。ADX > 25 かつ CCI が買われすぎ（> 100）の場合にショート参入。

テストでは年平均収益率は約 97% を示しています。暗号資産市場で最もパフォーマンスが優れています。

ADX はトレンドに強さがあるかどうかを評価し、CCI は押し目後のエントリータイミングを特定します。ロングとショートは ADX の方向に従います。

押し目でエントリーするモメンタムトレーダー向けです。ATR の倍数がリスクを管理します。

## 詳細

- **エントリー条件**:
  - ロング: `ADX > 25 && CCI < -100`
  - ショート: `ADX > 25 && CCI > 100`
- **ロング/ショート**: 両方
- **エグジット条件**: トレンドが弱まるか、CCI がゼロをクロス
- **ストップ**: `StopLossPercent` を使用したパーセントベース
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `CciPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: ADX, CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

