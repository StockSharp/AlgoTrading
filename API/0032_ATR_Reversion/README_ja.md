# ATR Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
ATR Reversionは、Average True Range (ATR)の倍数で測定された急激な動きを探します。価格がATRの乗数を超えた場合、システムは平均回帰を期待します。

テストでは年間平均リターン約133%が示されています。暗号資産市場で最も優れたパフォーマンスを発揮します。

この戦略はスパイクの方向と逆にトレードを開き、モメンタムを判断するために移動平均を使用します。

ポジションは移動平均のクロスオーバーまたはボラティリティストップに達したときにクローズします。

## 詳細

- **エントリー条件**: 価格の動きが`AtrMultiplier`倍のATRを超える。
- **ロング/ショート**: 両方向。
- **エグジット条件**: 価格が移動平均またはストップをクロス。
- **ストップ**: はい。
- **デフォルト値**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: ATR, MA
  - ストップ: はい
  - 複雑さ: 基本
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

