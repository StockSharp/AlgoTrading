# Crypto Scalper Momentum戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

## 概要

**Crypto Scalper Momentum戦略**は、Money Flow Index、Momentum、複数時間軸MACDフィルターを組み合わせ、MetaTraderの元のエキスパートアドバイザー「Crypto Scalper」を再現します。主な日中時間軸で動作し、上位時間軸で短期モメンタムを確認し、遅いMACDから得たマクロトレンドフィルターを尊重します。通貨ベースのバスケット目標、金額トレーリング、ブレイクイーブンストップ、エクイティドローダウン保護など、MQL実装の複数のリスク管理機能を保持しています。

## 取引ロジック

1. **主要指標**
   - 主時間軸のMoney Flow Index（MFI）。既定は14期間。
   - 主時間軸のMACD（12/26/9 EMA構成）。
2. **上位時間軸モメンタム**
   - 別のローソク足系列で計算するMomentum指標。MetaTraderの基準線（100）からの絶対距離が設定可能なしきい値を超える必要があります。
3. **マクロトレンドフィルター**
   - マクロ時間軸（既定は日足）で評価する遅いMACDが、上位トレンドに逆らう取引を防ぎ、反転時には強制決済します。
4. **エントリールール**
   - **ロング**: 直近3つのMFI値の少なくとも1つが売られ過ぎしきい値を下回り、モメンタム偏差がしきい値を超え、主MACDラインがシグナルラインを上回り、マクロMACDが強気である。
   - **ショート**: 買われ過ぎしきい値と弱気MACD確認を使った反対条件。
5. **決済ルール**
   - pipsで表す固定ストップロスとテイクプロフィット。
   - ローソク足の極値または古典的な距離ベースのトレールによる任意のトレーリングストップ。
   - 設定可能な有利方向への進行後のブレイクイーブン移動。
   - マクロMACDの反転は既存エクスポージャーを閉じます。
   - 通貨目標、パーセント目標、金額での利益トレーリングはMQL機能を再現します。
   - エクイティドローダウン監視は、口座がピークから指定パーセントだけ戻したとき、すべての取引を閉じます。

## リスク管理

- **ストップ/目標**: 任意で有効にできる設定可能なpip距離。
- **トレーリング**: ローソク足ベース（最近のローソク足の最安値/最高値）または古典的なpipトレーリング。
- **ブレイクイーブン**: トリガー距離に達すると、利益を固定するようストップを移動します。
- **資金管理**: 口座通貨でのバスケットテイクプロフィット、初期エクイティ比率、金額での利益トレーリング。
- **エクイティストップ**: 観測された最高エクイティを監視し、ドローダウンが許容率を超えると取引を閉じます。

## パラメーター

| 名前 | 説明 |
|------|-------------|
| `CandleType` | エントリーに使う主ローソク足系列。 |
| `MomentumCandleType` | Momentum指標へ供給する上位時間軸ローソク足。 |
| `MacroCandleType` | マクロMACDフィルター用の遅い時間軸ローソク足。 |
| `MfiPeriod` | Money Flow Indexの長さ。 |
| `MfiOversold` / `MfiOverbought` | オシレーターのしきい値（既定30 / 70）。 |
| `MomentumPeriod` | 上位時間軸のMomentum長。 |
| `MomentumThreshold` | モメンタムフィルターが要求する100ラインからの最小偏差。 |
| `MomentumReference` | 基準値（MetaTraderの既定値は100）。 |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | 取引時間軸のMACDパラメーター。 |
| `MacroMacdFastPeriod` / `MacroMacdSlowPeriod` / `MacroMacdSignalPeriod` | マクロ時間軸のMACDパラメーター。 |
| `TradeVolume` | 各成行注文の数量（ロット）。 |
| `MaxTrades` | 方向ごとの最大同時取引数（0 = 無制限）。 |
| `UseStopLoss` / `StopLossPips` | 保護ストップを有効化し設定します。 |
| `UseTakeProfit` / `TakeProfitPips` | 保護目標を有効化し設定します。 |
| `UseTrailingStop` | トレーリングロジックのメイン切り替え。 |
| `UseCandleTrail` | ローソク足極値トレーリングと古典的トレーリングを切り替えます。 |
| `TrailTriggerPips` / `TrailAmountPips` | 古典的トレーリングストップのトリガー距離と維持距離。 |
| `CandleTrailLength` / `CandleTrailBufferPips` | ローソク足ベーストレーリング用のローソク足数と追加バッファー。 |
| `UseBreakEven` / `BreakEvenTriggerPips` / `BreakEvenOffsetPips` | ブレイクイーブン起動距離と固定する利益。 |
| `UseMoneyTakeProfit` / `MoneyTakeProfit` | 口座通貨でのバスケットテイクプロフィット。 |
| `UsePercentTakeProfit` / `PercentTakeProfit` | 初期エクイティ比率でのバスケットテイクプロフィット。 |
| `EnableMoneyTrailing` / `MoneyTrailTarget` / `MoneyTrailStop` | 通貨での浮動利益トレーリング。 |
| `UseEquityStop` / `EquityRiskPercent` | 観測ピークに対するエクイティドローダウンガード。 |
| `ForceExit` | 次のローソク足終値でただちにポジションを解消します。 |

## 注記

- pip距離は銘柄の`PriceStep`で変換されます。ブローカーが価格ステップを提供しない場合は、MetaTraderのポイント処理に合わせて`0.0001`をフォールバックとして使います。
- マクロMACD購読は、元のEAを模倣するため月足へ向けることができます。すべてのデータフィードで月足バーが使えるとは限らないため、既定は日足です。
- リポジトリ規則に従い、コード内のコメントはすべて英語で書かれています。
