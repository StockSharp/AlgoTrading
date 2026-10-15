# Bago EA 戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略はMetaTraderの「Bago EA」エキスパートアドバイザーを複製します。移動平均とRSIのクロスオーバーで確認されたトレンドフォロー
ブレイクアウトで取引し、Vegasトンネル（EMA 144/169ペア）が空間フィルターとトレーリングアンカーを提供します。

## 取引ロジック

1. **インジケーター準備**
   - 2本のEMA（期間`FastPeriod`と`SlowPeriod`、メソッド`MaMethod`、価格`MaAppliedPrice`）。
   - Vegasトンネルの EMA（期間144と169、同じメソッド/価格）で方向チャンネルを検出。
   - RSI（`RsiPeriod`、`RsiAppliedPrice`）による確認。
   - すべての価格対pip変換は元のEAと同様に3/5桁調整付きの銘柄`PriceStep`を使用します。
2. **クロスオーバー状態マシン**
   - EMAの上下クロスおよびRSIの50上下クロスはタイマーで追跡されます。各状態は`CrossEffectiveBars`足間アクティブのままで、
     反対クロスまたはタイムアウトでリセットされます。
   - トンネルクロスは価格がVegasトンネルの一方から他方に移動するときをマークします。
3. **エントリー条件**
   - **ロング**：EMAとRSIの両方の上向きクロスがアクティブ *かつ* 価格が：
     - 少なくとも`TunnelBandWidthPips`だが`TunnelSafeZonePips`を超えずにトンネル上でクローズ、強気の足ボディあり、または
     - `TunnelBandWidthPips`だけトンネル下でクローズ、下からの反発を示す。
   - **ショート**：EMA/RSIの下向きクロスで鏡像ロジック。
   - 取引は有効なセッション内（ロンドン07–16、ニューヨーク12–21、東京00–08、または23:00以降にクローズする任意のバー）でのみ
     許可されます。
4. **注文処理**
   - 新規ポジションはボリューム`TradeVolume`で開かれます。反転前に反対ポジションが閉じられます。
   - 初期ストップはクローズ価格から`StopLossPips`に設定されます。ストップ対トンネルオフセットは`StopLossToFiboPips`を使用。
5. **トレーリングと部分決済**
   - 価格がVegasトンネルレベルを超えて前進するにつれてストップが動きます：
     - トンネル内では、ストップは`tunnel ± (TrailingStepX + StopLossToFibo)`に止まります。
     - トンネル外では、価格の後ろに`TrailingStopPips`のハードトレーリングが適用されます。
   - 部分決済は価格がエントリーから十分に動いたら`TrailingStep1Pips`で`PartialClose1Volume`、`TrailingStep2Pips`で
     `PartialClose2Volume`を閉じます。
   - 反対のEMA/RSIクロスは即座にポジション全体を閉じます。
6. **ストップ**
   - 保護注文は成行ストップ注文として維持されます。ポジションが閉じられるたびにキャンセルされます。

## パラメーター

| パラメーター | 型 | デフォルト | 説明 |
|------------|-----|----------|------|
| `TradeVolume` | decimal | 3 | ロット単位の注文サイズ。 |
| `StopLossPips` | decimal | 30 | 初期ストップロス距離。 |
| `StopLossToFiboPips` | decimal | 20 | Vegasトンネル周辺にストップを止める際の追加バッファ。 |
| `TrailingStopPips` | decimal | 30 | 価格がトンネルを離れた後のトレーリングストップ距離。 |
| `TrailingStep1Pips` | decimal | 55 | 部分決済とストップ再配置の最初の利益レイヤー。 |
| `TrailingStep2Pips` | decimal | 89 | 部分決済とトレーリングの2番目の利益レイヤー。 |
| `TrailingStep3Pips` | decimal | 144 | 純粋なトレーリングが使用される前の最終レイヤー。 |
| `PartialClose1Volume` | decimal | 1 | `TrailingStep1Pips`で閉じるボリューム。 |
| `PartialClose2Volume` | decimal | 1 | `TrailingStep2Pips`で閉じるボリューム。 |
| `CrossEffectiveBars` | int | 2 | EMA/RSIクロスが有効なままになる足数。 |
| `TunnelBandWidthPips` | decimal | 5 | 取引しないVegasトンネル周辺の中立ゾーン。 |
| `TunnelSafeZonePips` | decimal | 120 | ロングエントリーのトンネル上の最大距離（ショートはトンネル下）。 |
| `EnableLondonSession` | bool | true | ロンドン時間中のシグナルを許可。 |
| `EnableNewYorkSession` | bool | true | ニューヨーク時間中のシグナルを許可。 |
| `EnableTokyoSession` | bool | false | 東京時間中のシグナルを許可。 |
| `FastPeriod` | int | 5 | 高速EMAの長さ。 |
| `SlowPeriod` | int | 12 | 低速EMAの長さ。 |
| `MaShift` | int | 0 | すべてのEMAに適用される水平シフト。 |
| `MaMethod` | `MovingAverageType` | Exponential | 移動平均の平滑化モード。 |
| `MaAppliedPrice` | `AppliedPriceType` | Close | EMAに送られる足の価格。 |
| `RsiPeriod` | int | 21 | RSIの平均化長さ。 |
| `RsiAppliedPrice` | `AppliedPriceType` | Close | RSIに送られる足の価格。 |
| `CandleType` | `DataType` | H1タイムフレーム | 計算に使用する足シリーズ。 |

## 注意事項

- 戦略はトレーディング時間外でもインジケーター状態を維持します、元のEAとまったく同様に。
- ストップ注文は MetaTrader の`PositionModify`呼び出しを模倣するために高レベルAPI（`SellStop`/`BuyStop`）を通じて管理されます。
- すべてのコメントと構造はリポジトリガイドライン（インデントにタブ、英語インラインコメント）に従います。
