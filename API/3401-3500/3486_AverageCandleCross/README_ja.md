# 平均的なローソク足クロス戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、「平均ローソク足クロス」MetaTrader のエキスパートを再現しています。追加の 2 つの移動平均フィルターがすでに一般的なトレンドを確認している間、前のローソク足が移動平均を横切って閉じた完成したバーを待ちます。一度にアクティブにできるポジションは 1 つだけです。取引を開始した直後、アルゴリズムは指定されたピップベースのストップから距離が導出されるストップロスとテイクプロフィットを付加します。これにより、動作は小節ごとに 1 回起動する元のブロック ロジックと同じになります。

エントリーロジックは未完了のティックではなく過去の足データを読み取るため、すべてのシグナルは最後に完了したローソク足の終値で評価されます。個別のパラメーター セットは強気フィルターと弱気フィルターを制御し、非対称の平滑化や期間の長さを可能にします。保護レベルは、エントリー価格から離れた `StopLossPips * PipSize` に位置するネイティブのストップ注文と指値注文を使用して作成されます。テイクプロフィットでは同じ停止距離が再利用され、各サイドに定義されたパーセンテージ係数が乗算されます。

## 詳細

- **エントリー基準**:
  - **ロング**: ロングサイドの高速トレンドフィルターと低速トレンドフィルターは両方とも前のバー (`MA_fast1[1] > MA_slow1[1]` と `MA_fast2[1] > MA_slow2[1]`) で上昇しており、前のローソク足は専用の平均を上回って終了していますが、2 つのバーの前のローソク足はその平均を下回っていました (`Close[2] <= MA_cross[2]` と `Close[1] > MA_cross[1]`)。
  - **ショート**: ショートサイドの速いトレンドフィルターと遅いトレンドフィルターは両方とも前の足 (`MA_fast1[1] < MA_slow1[1]` と `MA_fast2[1] < MA_slow2[1]`) で下落しており、前のローソク足は専用平均を下回って終了していますが、2 つの足の前のローソク足はその平均値を上回っていました (`Close[2] >= MA_cross[2]` と `Close[1] < MA_cross[1]`)。
- **ロング/ショート**: 両方向ですが、同時にはできません。
- **終了基準**:
  - ポジションは保護的なストップロスまたはテイクプロフィット注文によってのみクローズされます。
- **ストップ**: はい。ストップはエントリー価格から `StopLossPips * PipSize` 離れたところに配置されます。テイクプロフィットは、停止距離に `% of SL` パラメータを乗算した値に等しくなります。
- **デフォルト値**:
  - `FirstTrendFastPeriod` = 5、`FirstTrendFastMethod` = SMA。
  - `FirstTrendSlowPeriod` = 20、`FirstTrendSlowMethod` = SMA。
  - `SecondTrendFastPeriod` = 20、`SecondTrendFastMethod` = SMA。
  - `SecondTrendSlowPeriod` = 30、`SecondTrendSlowMethod` = SMA。
  - `BullCrossPeriod` = 5、`BullCrossMethod` = SMA。
  - `BuyVolume` = 0.01、`BuyStopLossPips` = 50、`BuyTakeProfitPercent` = 100。
  - `FirstTrendBearFastPeriod` = 5、`FirstTrendBearFastMethod` = SMA。
  - `FirstTrendBearSlowPeriod` = 20、`FirstTrendBearSlowMethod` = SMA。
  - `SecondTrendBearFastPeriod` = 20、`SecondTrendBearFastMethod` = SMA。
  - `SecondTrendBearSlowPeriod` = 30、`SecondTrendBearSlowMethod` = SMA。
  - `BearCrossPeriod` = 5、`BearCrossMethod` = SMA。
  - `SellVolume` = 0.01、`SellStopLossPips` = 50、`SellTakeProfitPercent` = 100。
  - `PipSize` = 0.0001。
- **フィルター**:
  - カテゴリ: トレンドフォロー。
  - 方向: デュアル (ロング + ショート)。
  - インジケーター: 複数の移動平均。
  - ストップ: 固定ピップベースのストップと比例テイクプロフィット。
  - 複雑さ: 中程度。
  - 時間枠: 設定されたローソク足シリーズで動作します (デフォルトは 15 分)。
  - 季節性：いいえ。
  - ニューラルネットワーク: いいえ。
  - ダイバージェンス: いいえ。
  - リスクレベル：中。
