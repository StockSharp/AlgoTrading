# Adx Bollinger 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
ADXとBollingerバンドインジケーターに基づく戦略。ADX > 25かつ価格がBollingerバンド上限を突破したときロングエントリー。ADX > 25かつ価格がBollingerバンド下限を割り込んだときショートエントリー。

テストでは年平均リターン約115%を示しています。株式市場で最もパフォーマンスが高くなります。

ADXでフィルタリングされたBollingerバンドのブレイクにより、価格が勢いを持ってブレイクアウトしていることを確認します。システムはブレイクアウトの方向で取引します。

高ボラティリティ環境に適しています。ATRベースのストップが下方リスクを軽減します。

## 詳細

- **エントリー条件**:
  - ロング: `Close < LowerBand && ADX > 25`
  - ショート: `Close > UpperBand && ADX > 25`
- **ロング/ショート**: 両方
- **エグジット条件**: 価格が中央バンドに戻る
- **ストップ**: `AtrMultiplier`を使用したATRベース
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: ADX, Bollinger Bands
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

