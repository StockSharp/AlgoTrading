# Lego 4 Beta戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTraderスクリプト「exp_Lego_4_Beta」から変換されたモジュール式システムです。いくつかの一般的なテクニカルインジケーターを組み合わせ、パラメーターによって各コンポーネントを有効または無効にすることができます。

## アルゴリズム

1. **移動平均クロス** – 速い移動平均と遅い移動平均が計算されます。速い平均が遅い平均を上抜けするとロングポジションが開かれます。逆のクロスではショートポジションが開かれます。
2. **ストキャスティクスオシレーターフィルター** – 有効にすると、ロングエントリーにはストキャスティクス%K値が売られすぎレベルを下回ることが必要で、ショートエントリーには%Kが買われすぎレベルを上回ることが必要です。
3. **RSI決済** – 有効にすると、RSIが高い閾値を上回った場合に既存のロングポジションが決済されます。RSIが低い閾値を下回ったときにショートポジションが決済されます。

## パラメーター

- `UseMaOpen` – 移動平均クロスシグナルを有効にする。
- `FastMaLength` / `SlowMaLength` – 速い移動平均と遅い移動平均の長さ。
- `MaType` – 移動平均のタイプ（SMA、EMA、WMA）。
- `UseStochasticOpen` – エントリー用ストキャスティクスフィルターを有効にする。
- `StochLength` – ストキャスティクス計算のメイン期間。
- `StochKPeriod` / `StochDPeriod` – %Kと%Dラインの平滑化期間。
- `StochBuyLevel` / `StochSellLevel` – 売られすぎと買われすぎの閾値。
- `UseRsiClose` – RSIベースの決済を有効にする。
- `RsiPeriod` – RSI計算の長さ。
- `RsiHigh` / `RsiLow` – ポジション決済のためのRSI閾値。
- `CandleType` – サブスクライブするローソク足タイプ。

## 注意事項

この戦略はインジケーター値を処理するために高レベルの`SubscribeCandles`と`BindEx`を使用し、StockSharpの推奨スタイルに従っています。エントリーと決済には成行注文のみが使用されます。
