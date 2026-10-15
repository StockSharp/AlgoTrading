# モメンタム最小利益戦略に関する修士号
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要
この戦略は、モメンタム指標とモメンタム系列に基づいて計算される移動平均の間のクロスオーバーを取引することにより、MetaTrader 5 エキスパート アドバイザー **Momentum Min Profit.mq5** の MA を複製します。強気のシグナルは、前のバーが勢いをニュートラル 100 レベル以下に維持している間に、勢いが平均を上回ったときに表示されます。弱気シグナルは、モメンタムが平均を下回り、前のバーが 100 を超えたときに生成されます。実装では、元の金額ベースのエクイティ ストップとポイント単位で測定される固定のテイクプロフィット ディスタンスが維持されます。

## 取引ロジック
1. `CandleType` で定義されたローソク足をリクエストし、モメンタム インジケーターにフィードします。
2. `MomentumMovingAverageType` と `MomentumMovingAveragePeriod` で定義される移動平均を使用して、運動量ストリームを平滑化します。
3. 前のバーの値を使用してクロスオーバーを検出し、二重信号を回避します。
4. MQL バージョンのオプション機能:
   - 生成された信号の方向を反転します。
   - 新しい取引を開始する前に逆のエクスポージャーをクローズするか、エントリーを完全にスキップします。
   - いつでも単一のネット ポジションを強制します。
   - 完全に閉じたバーではなく、現在の (形成中の) ローソク足でトリガーできるようにします。
5. リスク管理を適用します。
   - 株式のストップインマネー: `PnL + Position * (close - PositionPrice)` は `StopLossMoney` を超えていなければなりません。
   - `Security.PriceStep` を通じて変換されたポイント単位のテイクプロフィット距離。

## パラメーター
| パラメータ | 種類 | デフォルト | 説明 |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | キャンドルは運動量の計算に使用されます。 |
| `MomentumPeriod` | `int` | `14` | モメンタムインジケーターのルックバック期間。 |
| `MomentumMovingAveragePeriod` | `int` | `6` | モメンタムに適用される移動平均の長さ。 |
| `MomentumMovingAverageType` | `MomentumMovingAverageType` | `Smoothed` | 移動平均アルゴリズム (単純、指数、平滑化、加重)。 |
| `ReverseSignals` | `bool` | `false` | MetaTrader の売買シグナルをミラーリングします。 |
| `CloseOpposite` | `bool` | `true` | 新しいポジションを開く前に、反対側のエクスポージャーを閉じます。 |
| `OnlyOnePosition` | `bool` | `true` | 単一のネットポジションを維持します。 |
| `UseCurrentCandle` | `bool` | `false` | 閉じたバーではなく、現在形成されているローソク足のシグナルを評価します。 |
| `StopLossMoney` | `decimal` | `15` | すべての取引を終了する前に、株式のドローダウンが許可されます。 |
| `TakeProfitPoints` | `decimal` | `460` | 商品ポイントでの利益目標（`PriceStep`を乗算）。 |
| `MomentumReference` | `decimal` | `100` | MQL 戦略からコピーされたニュートラルな運動量レベル。 |

## 実装メモ
- 移動平均は、StockSharp の組み込み SMA/EMA/SMMA/WMA クラスを再利用するために、`LengthIndicator<decimal>` インスタンスで実装されています。
- 元の注文キューとマジックナンバー フィルターは StockSharp のネット ポジションにマッピングされているため、この戦略は、`CloseOpposite` が有効な場合に反対側をフラットにし、新しいエクスポージャーをオープンするサイズの単一の成行注文を送信します。
- 株式保護は、浮動損失がしきい値に達すると、`CloseAll()` を介してすべてのポジションをクローズします。これは、手数料、スワップ、利益の合計を監視する MetaTrader の動作と正確に一致します。
