# NRTR Extr戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は追加シグナル矢印付きの **Nick Rypock Trailing Reverse** (NRTR) アルゴリズムを実装します。MQL5のオリジナル例「Exp_NRTR_extr」をStockSharp高レベルAPIに変換したものです。

## 動作方法

- カスタムの `NrtrExtrIndicator` は設定可能な期間にわたる平均レンジを計算し、価格に追従するトレーリングレベルを描画します。
- 価格がこのレベルを超えて反転すると、インジケーターは方向を変えて買いまたは売りシグナルを発します。
- 戦略は買いシグナルでロングポジションを、売りシグナルでショートポジションを建てます。
- 既存のポジションは反対シグナルで、または定義されたストップロスやテイクプロフィットレベルに達したときにクローズされます。

## パラメーター

| 名前 | 説明 |
| --- | --- |
| `Period` | 平均レンジ計算に使用するローソク足の数。 |
| `Digits Shift` | レンジファクターに適用する追加精度調整。 |
| `Stop Loss` | 価格ポイント単位の保護ストップ。 |
| `Take Profit` | 価格ポイント単位の利益目標。 |
| `Enable Buy Open` / `Enable Sell Open` | ロングまたはショートポジションの建玉を許可。 |
| `Enable Buy Close` / `Enable Sell Close` | 反対シグナルでの既存ポジションのクローズを許可。 |
| `Candle Type` | インジケーターに使用するローソク足の時間軸。 |

## 注意事項

インジケーターは市場のボラティリティを推定するためにAverage True Rangeに基づいています。可視化のため、戦略はチャートエリアにローソク足と実行された取引を自動的に描画します。

