# 三角形戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader エキスパートアドバイザー **Triangle v1** を StockSharp の高レベル API に移植したものです。元の EA は、ブレイクアウト型の注文を出す前に、上位時間枠の加重移動平均フィルター、momentum ダイバージェンス確認、非常に長期の MACD 確認を組み合わせていました。StockSharp 版はマルチタイムフレームのロジックを維持しつつ、ティックごとの資金管理をローソク足ベースの保護注文に置き換えています。

## 仕組み

1. **マルチタイムフレーム・フィルター。** 作業時間枠 (`CandleType`、デフォルト 15 分) は取引実行に使われます。トレンドと momentum のフィルターは、`T` を参照していた MQL 呼び出しを再現するため、上位時間枠 (`TrendCandleType`、デフォルト 1 時間) で計算されます。
2. **LWMAトレンドゲート。** 高速と低速の加重移動平均 (LWMA 相当) がそろっている必要があります。ロングセットアップでは高速 LWMA が低速 LWMA の上に留まる必要があり、ショートでは逆の関係が必要です。
3. **Momentum偏差。** 上位時間枠の 14 期間 momentum 系列は、直近 3 本の確定ローソク足のいずれかで中立レベル (100) から少なくとも `MomentumThreshold` だけ乖離している必要があり、`MomLevelB/MomLevelS` チェックを再現します。
4. **MACD確認。** 取引を許可する前に、非常に高い時間枠 (`MacdCandleType`、デフォルト 30 日足 ≈ 月足) の MACD メインラインがシグナルラインの正しい側にある必要があり、`MacdMAIN0` と `MacdSIGNAL0` の条件をコピーします。
5. **保護エグジット。** stop loss と take profit の距離は価格ステップで設定されます。確定バーでどちらかの水準に到達すると、戦略は成行注文でポジションを決済します。

## パラメーター

| パラメーター | 説明 |
| --- | --- |
| `FastMaPeriod`, `SlowMaPeriod` | 上位時間枠の加重移動平均の長さ。 |
| `MomentumPeriod` | 上位時間枠の momentum フィルター期間。 |
| `MomentumThreshold` | 直近 3 つの momentum 読み取り値のいずれかで必要な、100 からの最小絶対偏差。フィルターを無効にするには 0 に設定します。 |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | `MacdCandleType` に適用する MACD パラメーター。 |
| `StopLossSteps`, `TakeProfitSteps` | 商品の価格ステップ (ticks) で測定される保護ストップと目標距離。無効にするには 0 を使用します。 |
| `CandleType` | 注文実行に使用する取引時間枠。 |
| `TrendCandleType` | LWMA と momentum にデータを供給する上位時間枠。 |
| `MacdCandleType` | MACD 確認フィルターに使用する時間枠。 |

## 使用方法

1. 銘柄を選択し、分析したい時間枠に合わせて `CandleType`、`TrendCandleType`、`MacdCandleType` を設定します。
2. 別の市場やボラティリティ環境にシステムを適応させたい場合は、MA、momentum、MACD の長さを調整します。
3. 商品の tick サイズに応じて `StopLossSteps` と `TakeProfitSteps` を設定します。戦略はステップ数を実際の価格距離に自動変換します。
4. 戦略を開始します。必要なすべてのローソク足ストリームを購読し、高レベル `Bind` API でインジケーターを更新し、stop または目標に到達したときにポジションを管理します。

## 元のEAとの違い

- 金額ベースのエグジット (`Use_TP_In_Money`, `Use_TP_In_percent`) と残高保護ブロックは、StockSharp が商品単位で動作するため再作成していません。同等の動作は `StopLossSteps`/`TakeProfitSteps` を調整することで実現できます。
- EA の trailing-stop、break-even、equity-stop ロジックは、ティック処理と MetaTrader 固有の注文変更呼び出しに依存していました。移植版は明確さのため、より単純な固定ストップ方式を維持しています。必要に応じて、ユーザーは `UpdatePositionState` に trailing ルールを追加できます。
- 手動トレンドライン (`TREND`/`TRENDLOW`) と fractal 配列は、EA で裁量的フィルターとして使用されていました。StockSharp 戦略を完全に体系的に保つため、意図的に省略しています。
- EA は `Max_Trades` パラメーターを公開していましたが、戦略は通常の利用に合わせて常に最大 1 つのネットポジションだけを保持します。

取引する商品に合わせて、しきい値と時間枠パラメーターを調整してください。ボラティリティの高い市場では、小さな momentum 変動で除外されないよう、通常はより広い値が必要です。
