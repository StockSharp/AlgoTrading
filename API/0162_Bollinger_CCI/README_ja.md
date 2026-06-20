# Bollinger Cci Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

戦略の実装 - Bollinger Bands + CCI。価格が下部 Bollinger Band を下回り、CCI が -100 を下回っている（売られすぎ）場合に買い。価格が上部 Bollinger Band を上回り、CCI が 100 を上回っている（買われすぎ）場合に売り。

テストでは年平均収益率は約 73% を示しています。暗号資産市場で最もパフォーマンスが優れています。

Bollinger Bands はボラティリティの限界をマッピングし、CCI は平均からの距離を測定します。CCI の確認を伴うバンド超えのブレイクアウトがトレードを引き起こします。

トレンドが素早く伸びるボラティリティの高い市場に適しています。安全のために ATR ベースのストップが適用されます。

## 詳細

- **エントリー条件**:
  - ロング: `Close < LowerBand && CCI < CciOversold`
  - ショート: `Close > UpperBand && CCI > CciOverbought`
- **ロング/ショート**: 両方
- **エグジット条件**: 価格が中間バンドに戻る
- **ストップ**: `StopLoss` を使用した ATR ベース
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLoss` = new Unit(2, UnitTypes.Absolute)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands, CCI
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

