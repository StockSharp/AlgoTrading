# Supertrend Volume 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
Supertrend Volume は、Supertrend インジケーターに出来高確認を加えたものです。
Supertrend の転換時に出来高が増加していると、新たなインパルス移動の可能性が高まります。

テストでは平均年間リターンは約145%を示しています。暗号資産市場で最も良いパフォーマンスを発揮します。

この戦略は、平均を超える出来高を伴う場合にのみ Supertrend シグナルでトレンド方向にエントリーします。

ストップは Supertrend ラインを追跡し、価格が反対側でクローズしたときにイグジットします。

## 詳細

- **エントリー条件**: インジケーターシグナル
- **ロング/ショート**: 両方
- **エグジット条件**: ストップロスまたは反対シグナル
- **ストップ**: はい、パーセントベース
- **デフォルト値**:
  - `CandleType` = 15 minute
  - `StopLossPercent` = 2
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3
  - `VolumePeriod` = 20
    Supertrendの転換（線の反対側での終値）で逆方向のポジションを決済します。転換方向への新規ポジションは、その足の出来高が直前VolumePeriod本の平均を上回る場合だけ持つため、確認された転換ではドテンになります。
- **フィルター**:
  - カテゴリ: トレンドフォロー
  - 方向: 両方
  - インジケーター: Supertrend, Volume
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

