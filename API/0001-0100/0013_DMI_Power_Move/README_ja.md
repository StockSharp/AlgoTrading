# DMI Power Move 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
DMI（方向性移動指数）のパワームーブに基づく戦略

テストでは年平均リターンが約76%であることが示されています。外国為替市場で最もよく機能します。

DMI Power Moveは方向性指標の差異とADXを組み合わせて強力なトレンドを捉えます。+DIが-DIを大幅に上回る（またはその逆）かつADXが強い場合に取引に入ります。ADXが弱まるかDIの差が縮まると退場します。

このアプローチは強い方向性の動きと上昇するADXの両方を要求することで弱いシグナルを排除します。結果として取引数は減りますが、潜在的により高品質なトレンド取引が得られます。


## 詳細

- **エントリー条件**: ADX、ATR、DMIに基づくシグナル。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 逆シグナルまたはストップ。
- **ストップ**: はい。
- **デフォルト値**:
  - `DmiPeriod` = 14
  - `DiDifferenceThreshold` = 5m
  - `AdxThreshold` = 30m
  - `AdxExitThreshold` = 25m
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(15)
- **フィルター**:
  - カテゴリ: トレンド
  - 方向: 両方
  - インジケーター: ADX、ATR、DMI
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ (15m)
  - 季節性: いいえ
  - Neural Networks: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

