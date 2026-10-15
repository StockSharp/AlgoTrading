# ADX レンジブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、ADX が指定されたしきい値を下回って静かな市場を示している間に終値がルックバック期間の最高終値を上回ったときにロングポジションに参入します。取引は定義されたセッションと1日の最大取引数に制限されます。固定のストップロス（価格単位）がすべてのポジションを保護します。

## 詳細

- **エントリー条件**: セッション内で `Close >= previous highest close` かつ `ADX < threshold`
- **ロング/ショート**: ロングのみ
- **エグジット条件**: ストップロスまたはセッション終了
- **ストップ**: あり
- **デフォルト値**:
  - `AdxPeriod` = 14
  - `HighestPeriod` = 34
  - `AdxThreshold` = 17.5
  - `StopLoss` = 1000
  - `MaxTradesPerDay` = 3
  - `CandleType` = TimeSpan.FromMinutes(30)
- **フィルター**:
  - カテゴリ: ブレイクアウト
  - 方向: ロングのみ
  - インジケーター: ADX
  - ストップ: あり
  - 複雑さ: 初心者
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中
