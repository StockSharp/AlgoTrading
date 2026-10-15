# MBKAsctrend3戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MBKAsctrend3戦略は、異なる期間を持つ3つのWilliams %Rオシレーターを使用します。それらの加重組み合わせが市場トレンドを定義します。加重値が上限閾値を上抜け、かつ長期オシレーターも高い場合にロングポジションを建てます。値が下限閾値を下回るとショートポジションを建てます。ポジションはポイント単位で設定可能なストップロスとテイクプロフィットレベルで保護されます。

## 詳細
- **エントリー条件**:
  - **ロング**: Weighted WPR > 67+Swing かつ long WPR > 50-AverageSwing。
  - **ショート**: Weighted WPR < 33-Swing かつ long WPR < 50+AverageSwing。
- **ロング/ショート**: 両方。
- **エグジット条件**: 逆シグナルまたは保護レベル。
- **ストップ**: 絶対ストップロスとテイクプロフィット。
- **フィルター**: なし。

## パラメーター
- `WprLength1`, `WprLength2`, `WprLength3` – 3つのWilliams %Rインジケーターの期間。
- `Swing` – 上限/下限閾値のシフト。
- `AverageSwing` – 長期オシレーターに基づく追加シフト。
- `Weight1`, `Weight2`, `Weight3` – 各インジケーターの重み。
- `StopLoss`, `TakeProfit` – ポイント単位の保護レベル。
- `CandleType` – ローソク足の時間軸、デフォルト4時間。
