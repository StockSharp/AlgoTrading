# シンプル APF 戦略バックテスト
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、簡略化した自己相関価格予測（APF）モデルを実装しています。自己相関によって価格サイクルを検出し、直近リターンの線形回帰を使って将来の価格を予測します。予測利益が指定したしきい値を超えたときにロングポジションをオープンします。目標価格に達したときにポジションをクローズします。

## パラメーター

- `Length` – 自己相関と回帰に使用するバーの数。
- `Threshold Gain` – 取引に入るための最小期待価格上昇。
- `Signal Threshold` – 予測を保存するために必要な自己相関レベル。
- `Candle Type` – 計算に使用するローソク足の種類。
