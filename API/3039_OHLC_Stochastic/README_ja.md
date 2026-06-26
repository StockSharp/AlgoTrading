# OHLC Stochastic戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

OHLCローソク足でクラシックな%K/%D Stochasticオシレーターを使用するモメンタムフォロー戦略。
アルゴリズムは売られ過ぎ/買われ過ぎゾーンでのクロスオーバーに反応し、価格ステップで測定された設定可能なトレーリングストップでオープントレードを保護します。

## 詳細

- **コアアイデア**: Stochastic %Kが極端なレベルで%Dをクロスするときのモメンタムの転換を活用する。
- **エントリー条件**:
  - **ロング**:
    - %Kが%Dを上方クロスし、少なくとも1つのラインが`LevelDown`しきい値を下回る。
    - ショートポジションが存在する場合、クローズしてロングに反転。
  - **ショート**:
    - %Kが%Dを下方クロスし、少なくとも1つのラインが`LevelUp`しきい値を上回る。
    - ロングポジションが存在する場合、クローズしてショートに反転。
- **エグジット条件**:
  - トレーリングストップがヒットする（`TrailingStopSteps`距離と`TrailingStepSteps`改善要件に基づく）。
  - 反対のエントリーシグナルが現れ、リバーサルをトリガーする。
- **トレーリングロジック**:
  - 距離とステップはpips/ステップを絶対価格に変換するためにインストゥルメントの`PriceStep`で乗算されます。
  - ストップはエントリー価格から`TrailingStopSteps + TrailingStepSteps`を超えてトレードが移動した後にのみ前進します。
  - ロングとショートサイドのトレーリングロジックを分離。
- **インジケーター**:
  - 調整可能な`KPeriod`、`DPeriod`、`Slowing`を持つ[StochasticOscillator](https://doc.stocksharp.com/html/T_StockSharp_Algo_Indicators_StochasticOscillator.htm)。
- **ロング/ショート**: 両方。
- **ストップ**: トレーリングストップのみ（固定SL/TP注文なし）。
- **ポジションサイジング**: 戦略の`Volume`パラメータを使用；リバーサルは方向を変えるために`Volume + |Position|`を送信。
- **デフォルト値**:
  - `CandleType` = `TimeSpan.FromHours(12).TimeFrame()`
  - `KPeriod` = 5
  - `DPeriod` = 3
  - `Slowing` = 3
  - `LevelUp` = 70
  - `LevelDown` = 30
  - `TrailingStopSteps` = 5 (価格ステップ)
  - `TrailingStepSteps` = 2 (価格ステップ)
- **可視化**:
  - チャートが利用可能な場合、OHLCローソク足、Stochasticインジケーター、トレードマーカーを描画します。

## 使用上の注意事項

1. 戦略を開始する前に基礎となるインストゥルメントと時間軸を設定してください。
2. 実際のpip距離を反映するためにインストゥルメントのティックサイズに応じて`TrailingStopSteps`を調整してください。
3. 戦略は`StartProtection()`を呼び出し、追加のリスクルールを外部でアタッチできます。
4. Stochasticのリバーサルが価格をリードするトレンドレジームで最もよく機能します。
5. イントラデイ商品では、早期エグジットを避けるために低い時間軸でトレーリング距離を縮小する必要がある場合があります。
