# 初心者向けブレイクアウト戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

直近`Period`本のローソク足の最高値と最安値を使用してチャネルを形成します。終値が上限に近づくとロング、下限に近づくとショートになります。

## エントリー条件
- **ロング**: Close >= highest - (highest - lowest) * `ShiftPercent` / 100 かつトレンドがまだ上昇中でない。
- **ショート**: Close <= lowest + (highest - lowest) * `ShiftPercent` / 100 かつトレンドがまだ下降中でない。

## エグジット条件
- 反対のシグナルが現在のポジションを閉じ、逆方向に新しいポジションを開く。

## パラメーター
- `Period` – チャネル計算のための振り返りバー数。
- `ShiftPercent` – チャネル境界からのパーセンテージオフセット。
- `CandleType` – 作業ローソク足の時間軸。
- `Volume` – 取引量。
- `StopLoss` – 価格単位のストップロス。
- `TakeProfit` – 価格単位のテイクプロフィット。

## インジケーター
- Highest
- Lowest
