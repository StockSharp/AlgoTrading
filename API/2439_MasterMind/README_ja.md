# MasterMind戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

Stochasticオシレーターとウィリアムズ%Rを使用して、極端な売られすぎと買われすぎの状態を捉える戦略です。

## 概要
この戦略は2つのモメンタムインジケーターを監視します：
- **Stochastic Oscillator** ベース期間100、スムージング3/3。
- **Williams %R** 期間100。

Stochasticの%D値が3を下回り、Williams %Rが-99.9を下回ると売られすぎの市場を示し、ロングポジションを開きます。
Stochasticの%Dが97を上回り、Williams %Rが-0.1を上回ると買われすぎの市場を示し、ショートポジションを開きます。

トレードに入った後、アルゴリズムはストップロス、テイクプロフィット、トレーリングストップ、オプションのブレークイーブン移動でリスクを管理します。

## パラメーター
- `StochasticLength` – StochasticとWilliams %Rの計算期間。
- `StopLoss` – エントリー価格からのストップロス距離（ポイント）。
- `TakeProfit` – テイクプロフィット距離（ポイント）。
- `TrailingStop` – トレーリングの起動距離（ポイント）。
- `TrailingStep` – トレーリングストップのステップ（ポイント）。
- `BreakEven` – ストップをエントリーに移動する利益（ポイント）。
- `CandleType` – 戦略計算に使用するローソク足の時間軸。

## インジケーター
- `StochasticOscillator`
- `WilliamsR`

## 取引ルール
1. `%D < 3` かつ `Williams %R < -99.9` のとき**買い**。  
2. `%D > 97` かつ `Williams %R > -0.1` のとき**売り**。  
3. エントリー後、ストップロスとテイクプロフィットを適用する。  
4. 価格が`BreakEven`進んだらストップをエントリーに移動する。  
5. 価格が`TrailingStop`動いたらトレーリングストップを起動し、`TrailingStep`ずつ移動する。

## 注記
この戦略はStockSharpの高レベルAPIを使用しており、教育用の例として意図されています。
