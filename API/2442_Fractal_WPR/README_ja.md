# Fractal WPR 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はWilliams %Rオシレーターを使用し、買われすぎ・売られすぎレベルのクロスに基づいてトレードシグナルを生成します。MQL5エキスパートアドバイザーから採用されており、シンプルなモメンタムリバーサルシステムを示しています。

## 動作の仕組み

1. 選択した時間軸で設定可能な期間のWilliams %Rインジケーターが計算されます。
2. 2つの水平レベルが極端なゾーンを定義します：
   - `HighLevel` は買われすぎゾーンを示します（デフォルト −30）。
   - `LowLevel` は売られすぎゾーンを示します（デフォルト −70）。
3. `Trend` が `Direct` に設定されている場合：
   - `LowLevel` を下向きにクロスするとロングポジションを開き、ショートポジションをクローズします。
   - `HighLevel` を上向きにクロスするとショートポジションを開き、ロングポジションをクローズします。
4. `Trend` が `Against` に設定されている場合、クロスへの反応が逆になります。
5. オプションのパラメーターにより、ロングまたはショートポジションの開閉を個別に有効・無効にできます。
6. ティック単位のストップロスとテイクプロフィットの距離は、高レベル保護APIを使用して適用されます。

バー内のノイズへの反応を避けるため、完成したローソク足のみが処理されます。

## パラメーター

- `WprPeriod` – Williams %R の計算期間。
- `HighLevel` – 買われすぎゾーンのしきい値。
- `LowLevel` – 売られすぎゾーンのしきい値。
- `Trend` – トレードモード（`Direct` または `Against`）。
- `BuyPositionOpen` – ロングポジションの開設を許可。
- `SellPositionOpen` – ショートポジションの開設を許可。
- `BuyPositionClose` – ロングポジションのクローズを許可。
- `SellPositionClose` – ショートポジションのクローズを許可。
- `StopLossTicks` – ストップロス距離（ティック単位）。
- `TakeProfitTicks` – テイクプロフィット距離（ティック単位）。
- `CandleType` – 分析に使用するローソク足の時間軸。

## インジケーター

- Williams %R
