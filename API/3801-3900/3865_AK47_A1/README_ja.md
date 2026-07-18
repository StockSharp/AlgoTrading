# AK47A1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

「AK47_A1」MetaTrader エキスパートのポート。この戦略は、Bill Williams' Alligator、DeMarker オシレーター、Williams %R フィルター、フラクタル トリガーを組み合わせて、市場がレンジ条件を離れた場合にのみブレイクアウトを取引します。

## 詳細
- **データ**: `CandleType` によって定義された価格ローソク足。
- **指標**:
  - Alligator の顎/歯/唇は 13/8/5 周期の SMMA で、8/5/3 バー分シフトされ、中央値が供給されます。
  - 期間 13 の DeMarker は、買いの場合は 0.5 より長い側にあり、売りの場合は 0.5 未満である必要があります。
  - 期間 14 の Williams %R は `[0;1]` に正規化されます。買われすぎ/売られすぎの状態を避けるために、前のバーは 0.25 と 0.75 の間に留まる必要があります。
  - Fractals は最後の 5 つの高値と安値から検出され、3 つのバーの間有効です。
- **エントリー基準**:
  - 3 本の Alligator ラインはすべて、少なくとも `SpanGatorPoints` ポイントで区切られている必要があります（強気アライメントと弱気アライメントの両方）。
  - **ロング**: 最新の下位フラクタルは新鮮で、DeMarker ≥ 0.5 および Williams %R フィルターが取引を承認します。
  - **簡単**: 最新の上フラクタルは新鮮で、DeMarker ≤ 0.5 で、Williams %R フィルターが取引を承認します。
  - 反対のポジションは、新しいポジションをオープンする前にフラット化されます。
- **終了基準**:
  - `StopLossPoints` と `TakeProfitPoints` によって定義されるハードストップロスとテイクプロフィット（商品ステップを介して絶対価格に変換されます）。
  - ポジションが有利になると、終値を `TrailingStopPoints` ポイント追跡するオプションのトレーリング ストップ。
  - 逆シグナルが現れると、新しいポジションをオープンする前に現在のポジションがクローズされます。
- **デフォルト**:
  - `SpanGatorPoints` = 0.5
  - `TakeProfitPoints` = 100
  - `StopLossPoints` = 0 (無効)
  - `TrailingStopPoints` = 50
  - `CandleType` = 1 時間のキャンドル
