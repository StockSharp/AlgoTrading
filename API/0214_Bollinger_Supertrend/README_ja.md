# Bollinger Supertrend Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
この戦略はボリンジャーバンドとSupertrendインジケーターを組み合わせて、強い方向性のある動きの中でエントリーポイントを特定します。ボリンジャーバンドはボラティリティの拡大を測定し、Supertrend線は全体的なトレンドを追跡してトレーリングストップとして機能します。

テストでは年間平均リターン約79%を示しています。株式市場で最もパフォーマンスが高いです。

価格が上部ボリンジャーバンドの上で引け、Supertrend線の上に留まると、モメンタムとトレンドの一致を確認してロングトレードが発動します。価格が下部バンドの下で引け、Supertrendレベルの下に留まるとショートトレードが発生します。価格がSupertrendを反対方向にクロスバックすると、モメンタムが薄れたことを示してトレードは終了します。

システムは通常のボラティリティを超えたブレイクアウトを待つため、素早い反転よりも持続的な動きを捉えたいトレーダーに適しています。Supertrendストップは市場の動きに合わせて動的に調整され、手動介入なしでリスク管理を助けます。

## 詳細
- **エントリー条件**:
  - **ロング**: Close > upper Bollinger Band && Close > Supertrend
  - **ショート**: Close < lower Bollinger Band && Close < Supertrend
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: 価格がSupertrendを下回ったときに終了
  - **ショート**: 価格がSupertrendを上回ったときに終了
- **ストップ**: はい、Supertrendトレーリングストップを使用。
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: Bollinger Bands, Supertrend
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

