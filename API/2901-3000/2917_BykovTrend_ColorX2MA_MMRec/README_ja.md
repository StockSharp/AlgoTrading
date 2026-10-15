# BykovTrend + ColorX2MA MMRec 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この StockSharp 戦略は MQL5 エキスパート `Exp_BykovTrend_ColorX2MA_MMRec` を再現する。2つの独立したモジュールを組み合わせる：
Williams %R フィルターでローソク足を色付けする BykovTrend と、二重平滑化移動平均の傾斜を検査する ColorX2MA。
選択したモジュールが新しい色/傾斜変化を検出するたびにエントリーが発行され、資金管理は戦略のボリュームを使用するように
簡略化される。オプションのパーセンテージ・ストップロスとテイクプロフィットは StockSharp の組み込み保護ブロックを通じて有効化できる。

## 戦略ロジック

### BykovTrend モジュール
- `BykovTrendCandleType`（デフォルト2時間足）で計算された Williams %R（`BykovTrendWprLength`）を使用する。
- `BykovTrendRisk` は強気/弱気の閾値（`33 - Risk` と `-Risk`）を制御する。
- インジケーターの色はバー `BykovTrendSignalBar`（最後に閉じたバーからのシフト）で評価される。
- 強気色（< 2）は `AllowBykovTrendCloseSell` が有効の場合にショートを決済し、`EnableBykovTrendBuy` が真で前の色が強気でなかった場合にロングを開く可能性がある。
- 弱気色（> 2）は `AllowBykovTrendCloseBuy` が有効の場合にロングを決済し、`EnableBykovTrendSell` が真で前の色が弱気でなかった場合にショートを開く可能性がある。

### ColorX2MA モジュール
- `ColorX2MaCandleType` のローソク足を使用して、`ColorX2MaPriceType` で定義された価格に2段階のスムージング（`ColorX2MaMethod1`、`ColorX2MaLength1` と `ColorX2MaMethod2`、`ColorX2MaLength2`）を適用する。
- 第2段階の出力を前の値と比較して傾斜状態を生成する：上昇（1）、下降（2）、フラット（0）。
- 傾斜状態はバー `ColorX2MaSignalBar`（最後に閉じたバーからのシフト）で評価される。
- 上昇傾斜は `AllowColorX2MaCloseSell` によりショートを決済し、前の傾斜がまだ上昇していなかった場合に `EnableColorX2MaBuy` によりロングを開く可能性がある。
- 下降傾斜は `AllowColorX2MaCloseBuy` によりロングを決済し、前の傾斜がまだ下降していなかった場合に `EnableColorX2MaSell` によりショートを開く可能性がある。

### トレード管理
- クローズシグナルはオープンの前に実行され、オリジナルエキスパートの注文シーケンスをエミュレートする。
- 注文はポジションサイズとして `Strategy.Volume` を使用する。MQL バージョンの複雑な資金管理リカウンターは複製されない。
- `StopLossPercent` と `TakeProfitPercent` がゼロより大きい場合、パーセンテージベースのエグジットで `StartProtection` を有効化する。

## 詳細

- **ロング/ショート**: 両方向サポート。
- **エントリー条件**:
  - BykovTrend の強気色遷移。
  - ColorX2MA の上昇傾斜遷移。
- **エグジット条件**:
  - 有効なモジュールに応じた反対色/傾斜。
  - オプションのパーセンテージ・ストップロス/テイクプロフィット。
- **フィルター**: インジケーターロジック以外はなし。
- **ポジションサイジング**: `Strategy.Volume` による固定。

## パラメーター

| パラメーター | 説明 | デフォルト |
|------------|------|-----------|
| `EnableBykovTrendBuy` | BykovTrend によるロングトレードのオープンを許可する。 | `true` |
| `EnableBykovTrendSell` | BykovTrend によるショートトレードのオープンを許可する。 | `true` |
| `AllowBykovTrendCloseBuy` | BykovTrend が弱気になったときにロングを決済する。 | `true` |
| `AllowBykovTrendCloseSell` | BykovTrend が強気になったときにショートを決済する。 | `true` |
| `BykovTrendRisk` | Williams %R 感度（小さい値ほど速く反応する）。 | `3` |
| `BykovTrendWprLength` | Williams %R 期間。 | `9` |
| `BykovTrendSignalBar` | BykovTrend の色を評価するバーインデックス（シフト）。 | `1` |
| `BykovTrendCandleType` | BykovTrend のローソク足タイプ/時間軸。 | `2h` |
| `EnableColorX2MaBuy` | ColorX2MA によるロングトレードのオープンを許可する。 | `true` |
| `EnableColorX2MaSell` | ColorX2MA によるショートトレードのオープンを許可する。 | `true` |
| `AllowColorX2MaCloseBuy` | ColorX2MA の傾斜が弱気になったときにロングを決済する。 | `true` |
| `AllowColorX2MaCloseSell` | ColorX2MA の傾斜が強気になったときにショートを決済する。 | `true` |
| `ColorX2MaMethod1` | ステージ1の移動平均タイプ。 | `Simple` |
| `ColorX2MaLength1` | ステージ1スムージングの期間。 | `12` |
| `ColorX2MaPhase1` | ドキュメント用に保持されたフェーズプレースホルダー（未使用）。 | `15` |
| `ColorX2MaMethod2` | ステージ2の移動平均タイプ。 | `Jurik` |
| `ColorX2MaLength2` | ステージ2スムージングの期間。 | `5` |
| `ColorX2MaPhase2` | ドキュメント用に保持されたフェーズプレースホルダー（未使用）。 | `15` |
| `ColorX2MaPriceType` | ColorX2MA スムージングの価格ソース。 | `Close` |
| `ColorX2MaSignalBar` | 傾斜状態を評価するバーインデックス（シフト）。 | `1` |
| `ColorX2MaCandleType` | ColorX2MA のローソク足タイプ/時間軸。 | `2h` |
| `StopLossPercent` | オプションのパーセンテージ保護ストップ（0で無効化）。 | `0` |
| `TakeProfitPercent` | オプションのパーセンテージ保護テイクプロフィット（0で無効化）。 | `0` |

## 注意事項

- `ColorX2MaPhase1` と `ColorX2MaPhase2` はオリジナルのインプットを反映するために保持されているが、StockSharp の移動平均実装はフェーズパラメーターを公開しないため消費されない。
- StockSharp で利用可能なスムージングメソッドのみが提供される。サポートされていない SmoothAlgorithms オプションは最も近い類似物にフォールバックする。
- `TradeAlgorithms.mqh` の資金管理リカウンターは移植されていない。ポジションサイジングは外部のリスクコントロールまたは StockSharp のカスタムロジックで処理する必要がある。

## 使用方法

1. 目的の銘柄を割り当て、`Strategy.Volume` を取引したいロットサイズに設定する。
2. デフォルトの2時間時間軸が適切でない場合は、BykovTrend と ColorX2MA のローソク足タイプを設定する。
3. スムージングメソッド/レングスとシグナルバーオフセットをオリジナルのセットアップまたは独自のテストに合わせて調整する。
4. `StopLossPercent` と/または `TakeProfitPercent` をゼロより大きく設定することでオプションの保護ブロックを有効化する。
5. 戦略を開始する。設定されたローソク足ストリームにサブスクライブし、両モジュールを監視し、上記に定義された順序で成行注文を発行する。
