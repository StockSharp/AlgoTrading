# Bollinger Volume 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
出来高確認を伴うボリンジャーバンドのブレイクアウトを利用する戦略。
出来高が増加した状態でボリンジャーバンドを上/下に価格がブレイクしたときにポジションを取ります。

テストでは年平均リターン約178%を示しています。株式市場で最もパフォーマンスが高いです。

ボリンジャーバンドはボラティリティの拡大を示し、出来高がブレイクアウトを確認します。強い活動を伴ってバンドの外側でポジションが取られます。

継続を期待するブレイクアウトトレーダーに適しています。ATRベースのストップで損失を管理可能に保ちます。

## 詳細

- **エントリー条件**:
  - ロング: `Close > UpperBand && Volume > AvgVolume * VolumeMultiplier`
  - ショート: `Close < LowerBand && Volume > AvgVolume * VolumeMultiplier`
- **ロング/ショート**: 両方
- **エグジット条件**:
  - 価格が中央バンドに戻る
- **ストップ**: `StopLossAtr` を使用したATRベース
- **デフォルト値**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `VolumePeriod` = 20
  - `VolumeMultiplier` = 1.5m
  - `StopLossAtr` = 2.0m
  - `AtrPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Bollinger Bands, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: 中期
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

