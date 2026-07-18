# Jupiter M戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

MetaTraderエキスパート「Jupiter M. 4.1.1」から移植したグリッドベース戦略。
アルゴリズムは設定可能なステップを使用して注文バスケットを構築し、
新しいレベルが開かれるとテイクプロフィットとボリュームの両方を適応させます。

## 詳細

- **エントリー条件**：
  - ロング：価格がステップサイズだけ下落し（オプション）CCI < -100
  - ショート：価格がステップサイズだけ上昇し（オプション）CCI > 100
- **ロング/ショート**：両方
- **エグジット条件**：バスケットが計算されたテイクプロフィットに達する
- **ストップ**：指定されたステップ数後にブレークイーブン
- **デフォルト値**：
  - `TakeProfit` = 10
  - `FirstStep` = 20
  - `FirstVolume` = 0.01
  - `VolumeMultiplier` = 2
  - `CciPeriod` = 50
  - `CandleType` = 5分足
- **フィルター**：
  - カテゴリ：グリッド、平均回帰
  - 方向：両方
  - インジケーター：CCI（オプション）
  - ストップ：ブレークイーブン
  - 複雑さ：上級
  - 時間軸：イントラデイ
  - 季節性：いいえ
  - ニューラルネットワーク：いいえ
  - ダイバージェンス：いいえ
  - リスクレベル：高

## パラメーター

- `TakeProfit` – バスケットの価格単位での利益目標。
- `UseAverageTakeProfit` – オープン注文の平均価格からテイクプロフィットを計算。
- `DynamicTakeProfit` – `TpDynamicStep` 後に `TpDecreaseFactor` を使用してテイクプロフィットを削減、`MinTakeProfit` が下限。
- `BreakevenClose` / `BreakevenStep` – 指定ステップ数後に目標をブレークイーブンに移動。
- `FirstStep` – グリッドレベル間の初期距離。
- `DynamicStep`, `StepIncreaseStep`, `StepIncreaseFactor` – 追加注文ごとにステップを増加。
- `MaxStepsBuy` / `MaxStepsSell` – 方向ごとの最大注文数。
- `FirstVolume`, `VolumeMultiplier`, `MultiplyUseStep` – グリッドのボリューム成長を制御。
- `CciFilter` / `CciPeriod` – 最初の注文のためのオプションCCIフィルター。
- `AllowBuy` / `AllowSell` – 取引方向を有効化。
- `CandleType` – 計算用の足の時間軸。

この戦略はポジションを平均化して動的な利益目標でバスケットを決済することにより、
価格の平均回帰を捉えることを目的としています。
