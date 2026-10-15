# PresentTrend RMI Synergy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

標準Relative Momentum Indexと終値SMAによるエントリー後、保護方向にだけ動くSMA/ATR価格ストップで決済します。収益性は主張しません。

## 指標とエントリー

既定は確定した5分足です。標準RMIは`d[t]=close[t]-close[t-RmiMomentum]`、lag3を使います。直近`RmiPeriod=21`個の勢いのうち、Uは正のdの平均（その他はゼロ）、Dは負のdの絶対値の平均（その他はゼロ）です。`RMI=100*U/(U+D)`。分母ゼロならエントリー値はありません。標準の移動平均による相対モメンタム計算です。

終値SMAと標準Wilder ATRは`SuperTrendLength=5`を共有します。ATRは前回終値からのギャップを含み、拡大平均から始まり、その後Wilder平滑化を使います。3指標の形成後、ポジションなしからのみ入ります。RMI>60かつclose>SMAならロング、RMI<40かつclose<SMAならショート。`Volume`の成行注文です。保有中は新しいエントリーシグナルを無視します。

`Direction`は任意の型付き`Sides`フィルターです。C#の`null`/Pythonの`None`は双方向、`Sides.Buy`はロングのみ、`Sides.Sell`はショートのみ。エントリーを制限し、ストップは常に実際の保有側を閉じます。文字列や数値の方向コードは使いません。

## トレーリングストップ

`width=SuperTrendMultiplier*ATR`、既定倍率4。ロングは初期stop=`SMA-width`で、その後の各確定足で`max(previousStop,SMA-width)`に更新します。ショートは`SMA+width`で開始し、`min(previousStop,SMA+width)`に更新します。保有中は緩めません。初期値はシグナルのSMA/ATRから決まり、後の約定価格に依存しません。維持したSuperTrendパラメーター名はこのSMA中心の帯を設定します。

正のbid≤ロングstopまたは正のask≥ショートstopで実際の全量を成行決済します。更新後は確定closeも検査し、新stopに達した現在価格なら即時決済できます。足の間の実際の気配でも発動します。新stopを現在・以後の観測価格に適用し、その足の以前のhigh/lowには遡及適用しません。現在RMIが空でもSMA/ATRは保護を更新できます。

ローカル価格監視です。気配による足の間の保護にはLevel1 bid/askが必要で、なければcloseの検査が残ります。取引所のネイティブ逆指値を送信せず、約定価格はしきい値と異なる場合があります。未完了注文は重複を防ぎますが、指標とstopは進みます。決済足では再エントリーせず、後の条件を満たす足で可能です。EMA、6時間待機、反対シグナルによる即時反転、別の利確はありません。

| パラメーター | 既定値 |
|---|---:|
| `RmiPeriod` | 21、正 |
| `RmiMomentum` | 3、正の終値lag |
| `SuperTrendLength` | 5、正の共通SMA/ATR期間 |
| `SuperTrendMultiplier` | 4、正のATR幅 |
| `Direction` | 未設定、双方向 |
| `CandleType` | 5分 |

C#とPythonは既存BTC/TON履歴から勢いの和、SMA、Wilder ATR、ストップを独立計算します。各エントリー、執行可能価格での決済、実際のポジション、両方向とフィルター、反対シグナルの無視、狭い足間ストップ、6時間未満のエントリー間隔を検証します。
