# Color Zerolag TriX OSMA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

この戦略は、5つの異なるTRIX期間から構築されたゼロラグTRIX OSMAオシレーターを使用します。各TRIXコンポーネントは重み付けされ平滑化されて、最小限のラグでトレンド変化に反応する単一のオシレーターを形成します。オシレーターが上向きに転換するとロングポジションが開かれ、下向きに転換するとショートポジションが開かれます。

## 仕組み

1. 三重指数移動平均と変化率を使用して5つのTRIX値を計算します。
2. TRIXの値をそれぞれの重みと組み合わせて、高速トレンド値を形成します。
3. 高速トレンドを2回平滑化して、ゼロラグOSMAオシレーターを作成します。
4. 最後の2つのオシレーター値を比較してトレンド転換を検出します。
5. 上向き転換でロング、下向き転換でショートに入ります。新しいポジションを開く前に、既存の反対ポジションはクローズされます。

## パラメーター

- `Smoothing1` – 低速トレンドの平滑化係数。
- `Smoothing2` – OSMA ラインの平滑化係数。
- `Factor1..Factor5` – 各TRIXコンポーネントに適用される重み。
- `Period1..Period5` – 5つのTRIX計算の期間。
- `CandleType` – 計算に使用するローソク足シリーズ。

## インジケーター

- TripleExponentialMovingAverage
- RateOfChange
- カスタムゼロラグTRIX OSMA組み合わせ

## 注意事項

この戦略はシグナルを生成する前に5つのTRIXインジケーターすべてが形成されている必要があります。ストップとターゲットの保護は `StartProtection` を通じて有効化されます。
