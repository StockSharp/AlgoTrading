# MTC Combo v2戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTraderスクリプト「MTC Combo v2 (barabashkakvn's edition)」から変換。

## ロジック
- 移動平均の傾きを使用して基本的なトレンドを判断します。
- オプションのパーセプトロンフィルターは、設定可能なラグでの最近の始値差の加重和を計算します。
- `Pass` パラメーターが使用するパーセプトロンの分岐を選択します：
  - 4：ロングにはperceptron3 > 0かつperceptron2 > 0が必要；ショートにはperceptron3 <= 0かつperceptron1 < 0。
  - 3：ロングにperceptron2 > 0を使用。
  - 2：ショートにperceptron1 < 0を使用。
  - その他の値：MAの傾きのみに基づいて取引。

ストップロスとテイクプロフィットのレベルは `Sl*` と `Tp*` パラメーターから取得されます。

## パラメーター
- `MaPeriod` – 移動平均の長さ。
- `P2`, `P3`, `P4` – パーセプトロンのラグ。
- `Pass` – 決定モード。
- `Sl1`/`Tp1`, `Sl2`/`Tp2`, `Sl3`/`Tp3` – 各分岐のストップとターゲット。
- `CandleType` – 処理するローソク足シリーズ。

## 注記
この戦略は一度に1つのポジションのみを保持し、ストップロスまたはテイクプロフィットに達するとクローズします。

## 免責事項
教育目的のみ。投資アドバイスではありません。
