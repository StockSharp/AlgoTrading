# バウンス・ストレングス・インデックス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はBounce Strength Index（BSI）の簡略版を実装します。このインジケーターは、価格が直近のレンジ内でどこに引けるかを測定し、モメンタムの転換を強調するために二重の平滑化を適用します。

## ロジック
- **Highest** と **Lowest** インジケーターを使用して直近の高値と安値を計算します。
- そのレンジ内での終値の位置を決定し、**SimpleMovingAverage** で結果を2回平滑化します。
- インジケーターが上向きに転換すると、ショートポジションをクローズしてロングポジションを開きます。
- インジケーターが下向きに転換すると、ロングポジションをクローズしてショートポジションを開きます。

## パラメーター
- `CandleType` – 分析に使用するローソク足シリーズ。
- `RangePeriod` – レンジ計算のルックバック期間。
- `Slowing` – 高速平滑化の長さ。
- `AvgPeriod` – 低速平滑化の長さ。

## インジケーター
- BounceStrengthIndex（カスタム）
- Highest
- Lowest
- SimpleMovingAverage
