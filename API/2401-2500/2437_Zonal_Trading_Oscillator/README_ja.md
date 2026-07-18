# Zonal Trading オシレーター戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Zonal Trading戦略は、Bill Williamsのクラシックな「ゾーン」コンセプトを再現しています。Awesome Oscillator（AO）とAccelerator Oscillator（AC）の色を監視します。緑のバーはオシレーターの値が前のバーより増加したことを意味し、赤のバーは減少したことを意味します。両方のオシレーターが緑になると戦略はロングポジションを開きます。両方が赤になるとショートポジションを開きます。反対の色が出ると既存のポジションを決済します。

## 詳細
- **エントリー条件**:
  - **ロング**: AOが増加し、ACが増加する。
  - **ショート**: AOが減少し、ACが減少する。
- **エグジット条件**:
  - **ロング**: AOまたはACが減少する。
  - **ショート**: AOまたはACが増加する。
- **ストップ**: デフォルトではなし。
- **パラメーター**:
  - `AoCandleType` – Awesome Oscillatorの時間軸（デフォルト`H4`）。
  - `AcCandleType` – Accelerator Oscillatorの時間軸（デフォルト`H4`）。
  - `BuyOpen`, `SellOpen` – ロングおよびショートエントリーを有効/無効にする。
  - `BuyClose`, `SellClose` – ロングおよびショートポジションのエグジットを有効/無効にする。
- **インジケーター**: Awesome Oscillator (5/34)、Accelerator Oscillator（AO マイナス SMA(5)）。
- **タイプ**: モメンタムフォロー。オシレーターが利用可能なあらゆる市場と時間軸で機能します。
