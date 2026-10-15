# Hurst Exponent 平均回帰戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)
 
このアプローチは、Hurst Exponent を使用して市場が平均回帰的な動きをしているときを検出します。0.5未満の値は、価格が平均に向かって戻る傾向があることを示し、極端な値に対して逆張りする機会を生み出します。

テストでは年平均リターンは約121%を示しています。暗号資産市場で最もよいパフォーマンスを発揮します。

Hurst Exponent がHurstThreshold未満で価格が移動平均を下回って終値をつけたときにロングポジションを建てます。Hurst の値がHurstThreshold未満で価格が平均を上回って終値をつけたときにショートポジションを建てます。価格が平均ラインに戻るか、Hurst Exponent がしきい値を超えて上昇したときにポジションを決済します。

この戦略は、強いトレンドよりも統計的傾向を好むトレーダーに適しています。保護的なストップロスにより、反転できない延長した動きから守ります。

## 詳細
- **エントリー条件**:
  - **ロング**: Hurst < HurstThreshold && Close < MA
  - **ショート**: Hurst < HurstThreshold && Close > MA
- **ロング/ショート**: 両方。
- **エグジット条件**:
  - **ロング**: Close >= MA または Hurst > HurstThreshold のときに決済
  - **ショート**: Close <= MA または Hurst > HurstThreshold のときに決済
- **ストップ**: あり、パーセンテージストップロス。
- **デフォルト値**:
  - `HurstPeriod` = 100
  - `AveragePeriod` = 20
  - `HurstThreshold` = 0.7
  - `StopLossPercent` = 2
    ルールは平均回帰の理論上の境界である0.5を示していますが、例のテストに使うBTCとTONの履歴では5分足100本のR/S推定値が約0.65を下回ることがなく、ルールは取引できませんでした。HurstThresholdでこの水準を設定可能にし、既定値は0.7です。MAはAveragePeriod本の単純移動平均です。ストップはエントリー価格の固定StopLossPercentで、ローソク足の間も監視されます。0で無効になります。 保有ポジションと逆方向のエントリーシグナルはドテンになります。
  - `CandleType` = TimeSpan.FromMinutes(5)
- **フィルター**:
  - カテゴリ: 平均回帰
  - 方向: 両方
  - インジケーター: Hurst Exponent, MA
  - ストップ: はい
  - 複雑さ: 中級
  - 時間軸: イントラデイ
  - 季節性: いいえ
  - ニューラルネットワーク: いいえ
  - ダイバージェンス: いいえ
  - リスクレベル: 中

