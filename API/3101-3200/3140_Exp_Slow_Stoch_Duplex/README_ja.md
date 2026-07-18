# Exp Slow Stoch Duplex戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は MetaTrader 5 エキスパートアドバイザー **Exp_Slow-Stoch_Duplex** の StockSharp 高レベルポートです。独立した時間軸で機能する 2 つのスロー Stochastic オシレーターを組み合わせて、協調したロングとショートのシグナルを生成します。各オシレーターは独自のクロスオーバーシグナルを提供し、保護注文が元のストップロスとテイクプロフィット管理をエミュレートしながら、戦略が方向性ポジションを開閉できるようにします。

## 取引ルール

- **ロングモジュール**
  - `LongCandleType` 時間軸でロング Stochastic を評価します。
  - 設定された平滑化メソッドを %K と %D の値に適用し、`LongSignalBar` バー分シフトします。
  - %K が %D を上回るとき（`previousK <= previousD` かつ `currentK > currentD`）ロングポジションを開きます。
  - %K が %D を下回ると（`currentK < currentD`）既存のロングポジションを閉じます。
- **ショートモジュール**
  - `ShortCandleType` 時間軸でショート Stochastic を評価します。
  - %K が %D を下回るとき（`previousK >= previousD` かつ `currentK < currentD`）ショートポジションを開きます。
  - %K が %D を上回ると（`currentK > currentD`）既存のショートポジションを閉じます。
- 注文は成行注文で実行されます。送られる出来高は `TradeVolume` に現在のポジションの絶対値を加えたもので、反転が最初に前のエクスポージャーを均せるようにします。
- MT5 の注文パラメーターを模倣するために、価格ポイントで表現された保護的なテイクプロフィットとストップロスが `StartProtection` を通じて付加されます。

## パラメーター

| パラメーター | 型 | デフォルト | 説明 |
|------------|----|-----------|----|
| `LongCandleType` | `DataType` | 8 時間足ローソク | ロング Stochastic オシレーターの時間軸。 |
| `LongKPeriod` | `int` | 5 | ロング Stochastic の %K 計算期間。 |
| `LongDPeriod` | `int` | 3 | ロング Stochastic の %D 平滑化期間。 |
| `LongSlowing` | `int` | 3 | Stochastic 計算内に適用される追加の遅延。 |
| `LongSignalBar` | `int` | 1 | クロスオーバー評価に使用する閉じたバーの数。 |
| `LongSmoothingMethod` | `SmoothingMethod` | `Smoothed` | %K と %D に適用するセカンダリ平滑化（None、Simple、Exponential、Smoothed、Weighted）。 |
| `LongSmoothingLength` | `int` | 5 | ロングオシレーターのセカンダリ平滑化フィルターの長さ。 |
| `LongEnableOpen` | `bool` | `true` | 戦略がロングポジションを開くことを許可する。 |
| `LongEnableClose` | `bool` | `true` | 戦略がロングポジションを閉じることを許可する。 |
| `ShortCandleType` | `DataType` | 8 時間足ローソク | ショート Stochastic オシレーターの時間軸。 |
| `ShortKPeriod` | `int` | 5 | ショート Stochastic の %K 計算期間。 |
| `ShortDPeriod` | `int` | 3 | ショート Stochastic の %D 平滑化期間。 |
| `ShortSlowing` | `int` | 3 | Stochastic 計算内に適用される追加の遅延。 |
| `ShortSignalBar` | `int` | 1 | ショートクロスオーバー評価に使用する閉じたバーの数。 |
| `ShortSmoothingMethod` | `SmoothingMethod` | `Smoothed` | ショート %K と %D 値に適用するセカンダリ平滑化。 |
| `ShortSmoothingLength` | `int` | 5 | ショートオシレーターのセカンダリ平滑化フィルターの長さ。 |
| `ShortEnableOpen` | `bool` | `true` | 戦略がショートポジションを開くことを許可する。 |
| `ShortEnableClose` | `bool` | `true` | 戦略がショートポジションを閉じることを許可する。 |
| `TradeVolume` | `decimal` | 0.1 | ポジションエントリーの基本出来高。 |
| `TakeProfitPoints` | `decimal` | 2000 | 価格ポイントで表現されたテイクプロフィット距離。 |
| `StopLossPoints` | `decimal` | 1000 | 価格ポイントで表現されたストップロス距離。 |

## ノート

- 追加の `SmoothingMethod` は、StockSharp で利用可能な標準移動平均を使用して元のインジケーターのオプションの JJMA ベースの平滑化を模倣します。厳密な複製が必要でない場合は、`None` を選択してこの段階を無効にしてください。
- ロングとショートのモジュールは独立しています；対応する boolean フラグを使用していずれかの側を有効または無効にできます。
- StockSharp はネットポジションで動作するため、新しいシグナルが方向を逆転させると戦略は常に逆のエクスポージャーを閉じます。
