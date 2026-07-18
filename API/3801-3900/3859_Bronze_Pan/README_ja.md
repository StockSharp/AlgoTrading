# ブロンズパン戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader 4 エキスパート アドバイザー「Bronzew_pan」の StockSharp 移植です。完成したローソク足で単一の商品を取引し、独自の DayImpuls オシレーターと Williams %R および商品チャネル指数 (CCI) を組み合わせて、勢いの反転を検出します。

## 仕組み

1. 構成されたローソク足タイプをサブスクライブし、同じ期間で DayImpuls、Williams %R、および CCI を実行します。
2. 元のヘッジ動作をエミュレートするために、長期エクスポージャーと短期エクスポージャーを独立して会計処理します。
3. 変動利益が `ProfitTarget` に達するか、`LossTarget` を下回ると、すべてのポジションをクローズします。
4. DayImpuls が `DayImpulsShortLevel` を上回って下落し、Williams %R が `WilliamsLevelUp` を上回り、CCI が `CciLevel` を超えた場合にショートが始まります。
5. DayImpuls が `DayImpulsLongLevel` を下回って上昇し、Williams %R が `WilliamsLevelDown` を下回り、CCI が `-CciLevel` 未満の場合にロングが始まります。
6. 変動損益が `PredBand` の範囲を超えた場合、戦略は `LotMultiplier` を乗算した大きな平均注文を送信して方向を反転し、MetaTrader からの緊急回復ロジックを反映します。
7. 個々のストップロスとテイクプロフィットの値は、価格に変換されたピップ距離を使用してロングバスケットとショートバスケットで監視されます。
8. アカウント残高が `MinimumBalance` を下回っている場合、またはロング バスケットとショート バスケットの両方がアクティブな場合、新しい取引は開始されません。

## パラメーター

| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `TradeVolume` | エントリの基本ボリューム。 | `0.1` |
| `LongStopLossPips` | ロングバスケットのストップロス距離 (ピップ単位)。 | `0` |
| `ShortStopLossPips` | ショートバスケットのストップロス距離 (ピップ単位)。 | `0` |
| `LongTakeProfitPips` | ロングバスケットのテイクプロフィット距離（ピップ単位）。 | `0` |
| `ShortTakeProfitPips` | ショートバスケットのテイクプロフィット距離（ピップ単位）。 | `0` |
| `IndicatorPeriod` | DayImpuls、Williams %R および CCI で使用される長さ。 | `14` |
| `CciLevel` | 買われ過ぎ/売られ過ぎを確認する絶対的な CCI しきい値。 | `150` |
| `WilliamsLevelUp` | Williams ショートパンツには %R レベルが必要です。 | `-15` |
| `WilliamsLevelDown` | ロングには Williams %R レベルが必要です。 | `-85` |
| `DayImpulsShortLevel` | 短いエントリーを可能にする DayImpuls レベル。 | `50` |
| `DayImpulsLongLevel` | 長いエントリを可能にする DayImpuls レベル。 | `-50` |
| `ProfitTarget` | すべてのポジションを決済する変動利益。 | `500` |
| `LossTarget` | すべてのポジションをクローズする浮動損失。 | `-2000` |
| `PredBand` | 平均反転をトリガーするために使用される利益バンド。 | `100` |
| `LotMultiplier` | 反転中にベースボリュームに適用される乗数。 | `30` |
| `MinimumBalance` | 取引を続けるために必要な最低限の口座残高。 | `3000` |
| `CandleType` | キャンドルのサブスクリプションに使用される時間枠。 | `15m` |

## 注意事項

- DayImpuls オシレーターは、ポイントで表現されたローソク体を平滑化するオリジナルのダブル EMA を複製します。
- ストップロスとテイクプロフィットの値はオプションです。 `0` を設定すると、それぞれの保護側が無効になります。
- この戦略は完成したローソク足に依存し、不完全なバーを無視します。
