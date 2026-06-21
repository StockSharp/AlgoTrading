# ゲーム理論取引戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

ゲーム理論取引戦略は、群集行動分析、流動性トラップの検出、機関投資家のフロー、ナッシュ均衡ゾーンを組み合わせ、逆張りとモメンタムの動きを取引します。

RSIの極値と出来高スパイクを監視して群集の買いや売りを検出します。直近の高値・安値付近の流動性トラップ、売買代金累計（A/D）、スマートマネーの偏りがエントリーを絞り込みます。移動平均と標準偏差から構築した価格帯がナッシュ均衡を定義し、リバーサル取引に活用されます。価格が均衡に近い場合や機関投資家の出来高が現れた場合にポジションサイズが調整されます。

## 詳細
- **データ**: 価格と出来高のローソク足。
- **エントリー条件**: 逆張り、モメンタム、またはナッシュリバーサルシグナル。
- **エグジット条件**: ストップロス / テイクプロフィット、または反対シグナル。
- **ストップ**: オプションのストップロスとテイクプロフィット。
- **デフォルト値**:
  - `RsiLength` = 14
  - `VolumeMaLength` = 20
  - `HerdThreshold` = 2.0
  - `LiquidityLookback` = 50
  - `InstVolumeMultiplier` = 2.5
  - `InstMaLength` = 21
  - `NashPeriod` = 100
  - `NashDeviation` = 0.02
  - `UseStopLoss` = True
  - `StopLossPercent` = 2
  - `UseTakeProfit` = True
  - `TakeProfitPercent` = 5
- **フィルター**:
  - カテゴリ: 混合逆張り/モメンタム
  - 方向: ロング & ショート
  - インジケーター: RSI, SMA, Accumulation/Distribution, StandardDeviation, Highest/Lowest
  - 複雑さ: 上級
  - リスクレベル: 中
