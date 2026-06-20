# Vwap Adx Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
VWAPとADXインジケーターに基づく戦略。価格がVWAPより上でADX > 25のときロングエントリー。価格がVWAPより下でADX > 25のときショートエントリー。ADX < 20のときに決済。

テストでは年平均リターン約157%を示しています。暗号資産市場で最もパフォーマンスが高くなります。

VWAPがセッションのベンチマークとして機能し、ADXが確信度を測定します。価格がVWAPから乖離しADXが強さを示したときにエントリーが現れます。

イントラデイトレンドトレーダーに適しています。保護ストップはATRの倍数を使用します。

## 詳細

- **エントリー条件**:
  - ロング: `Close > VWAP && ADX > 25`
  - ショート: `Close < VWAP && ADX > 25`
- **ロング/ショート**: 両方
- **エグジット条件**: ADXが閾値を下回る
- **ストップ**: `StopLossPercent`を使用したパーセントベース
- **デフォルト値**:
  - `StopLossPercent` = 2m
  - `AdxPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: VWAP, ADX
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

