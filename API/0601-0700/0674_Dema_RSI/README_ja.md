# DEMA RSI戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は二重指数移動平均のRSIとその平滑化値のクロスオーバーを取引します。RSIが平滑化ラインを上抜けたときにロングポジションを建て、逆のクロスオーバーでショートポジションを建てます。ポジションはテイクプロフィット、トレーリングストップ、およびオプションの取引セッションフィルターで保護できます。

## パラメーター
- ローソク足タイプ
- MA期間
- RSI期間
- RSI平滑化期間
- テイクプロフィット（ポイント）
- トレーリングストップ（ポイント）
- セッションを使用する
- セッション開始
- セッション終了

### RSI(DEMA) の実際の売買条件

既定値: `CandleType=1m`、`MaLength=20`、`RsiLength=14`、`RsiSmoothingLength=5`、`TakeProfitPoints=0`、`TrailStopPoints=0`、`UseSession=false`、`SessionStart=09:00`、`SessionEnd=17:00`。確定足から `EMA1=EMA(Close,20)`、`EMA2=EMA(EMA1,20)`、`DEMA=2×EMA1−EMA2`、`R=RSI(DEMA,14)`、`S=SMA(R,5)` を計算します。前回 `R <= S` から今回 `R > S` に抜けたら買い、前回 `R >= S` から今回 `R < S` なら売り。ノーポジションからの新規、または `Volume + abs(Position)` による反転です。`UseSession=true` ならエントリーは `[SessionStart,SessionEnd)` に限定（深夜越え対応）、保護決済は時間制限を受けません。`TakeProfitPoints` と `TrailStopPoints` は**商品の最小価格刻み**単位で、正の値はネイティブの絶対値幅による利確とトレーリング損切りを有効化し、0は各機能を無効化します。元のREADMEは数値既定値を示しておらず、ここで明示しました。以前のEMA120/450クロスは本戦略ではありません。

