# Trickerless RHMP 戦略 (StockSharp ポート)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

この戦略は、MetaTrader エキスパート アドバイザー **Trickerless RHMP** を StockSharp の高レベル API に移植します。多段階を維持します
オリジナルロボットのエントリーロジック - 平均方向指数確認、平滑化移動平均構造、および
ボラティリティ主導のポジション管理 - `AGENTS.md` に記載されているフレームワーク規則に従います。

## 取引ロジック

1. **指標**
   - 平均トゥルー レンジ (ATR) とボラティリティ サイズの設定可能な期間。
   - トレンドの強さを評価するための完全な +DI/-DI コンポーネントを含む平均方向性インデックス (ADX)。
   - 高速トレンド フィルターと低速トレンド フィルターを表す 2 つの平滑移動平均 (SMMA)。

2. **トレンド評価**
   - 遅い SMMA の傾きは、`MinSlopePips`…`MaxSlopePips` コリドー内にある必要があります (機器ピップで測定)。
   - ADX は `AdxThreshold` を超え、前のローソク足と比較して上昇する必要があります。
   - 混雑を避けるために、料金は高速 SMMA から少なくとも `TrendSpacePips` 離れた場所にある必要があります。
   - 強気バイアスには、低速 SMMA を上回る高速 SMMA、+DI ≧ -DI、および高速平均の上昇が必要です。弱気バイアスはこれらを反映しています
小切手。

3. **一次エントリ**
   - 強気 (または弱気) バイアスが有効な場合、戦略は、次の条件を考慮して、ボリューム `OrderVolume` でロング (またはショート) 注文をオープンします。
`MaxNetPositions` し、エントリ間は少なくとも `SleepInterval` 待機します。
   - 逆のネット ポジションが存在する場合は、ヘッジを無効のままにするために、最初にフラット化されます。

4. **スパイクエントリー**
   - 現在のローソク足の範囲が前の範囲の `CandleSpikeMultiplier` 倍を超えている場合、戦略は補助的なローソク足を発動できます。
ADX コンポーネントが一致すると、キャンドル本体の方向の位置が決まります。位置には `OrderVolume * SpikeVolumeMultiplier` が使用されます。

## リスク管理

- ATR ベースのストップロス、テイクプロフィット、およびオプションのトレーリングストップ (`StopLossAtrMultiplier`、`TakeProfitAtrMultiplier`、`TrailingAtrMultiplier`)。
- セッション全体の保護: 実現された損益が `DailyProfitTarget` (開始資本の一部) に達すると、新しいエントリーはブロックされます。
- グローバル緊急スイッチ `EmergencyExit` は、切り替えるとすぐにすべてのポジションを閉じます。

## パラメーター

| 名前 | 説明 | デフォルト |
| --- | --- | --- |
| `CandleType` | すべての計算に使用される時間枠。 | 5分キャンドル |
| `OrderVolume` | 各エントリの基本ボリューム。 | 0.03 |
| `AtrPeriod` | ATR ルックバックの長さ。 | 14 |
| `AdxPeriod` | ADX ルックバックの長さ。 | 14 |
| `AdxThreshold` | 取引を可能にする最小の ADX 値。 | 10 |
| `FastMaPeriod` | 高速平滑移動平均期間。 | 60 |
| `SlowMaPeriod` | ゆっくりとした平滑化移動平均期間。 | 120 |
| `MinSlopePips` / `MaxSlopePips` | 低速 SMMA に許可されたスロープコリドー。 | 2/9 |
| `TrendSpacePips` | 高速 SMMA からの最小価格距離 (ピップ単位)。 | 5 |
| `CandleSpikeMultiplier` | スパイクエントリーをトリガーするには、ローソク足の範囲がどのくらい大きくなければならないか。 | 7 |
| `TakeProfitAtrMultiplier` | 利食いの場合は ATR 倍です。 | 1.0 |
| `StopLossAtrMultiplier` | ストップロスの倍数は ATR です。 | 1.5 |
| `TrailingAtrMultiplier` | トレーリングストップの ATR の倍数 (0 は無効になります)。 | 0 |
| `MaxNetPositions` | 同時ネットポジションユニットの最大数。 | 1 |
| `SleepInterval` | 連続するエントリ間の最小時間。 | 24分 |
| `DailyProfitTarget` | 初期資本のうち、到達すると取引をブロックする割合。 | 0.045 |
| `AllowNewEntries` | エントリを有効/無効にするマスタースイッチ。 | 本当の |
| `SpikeVolumeMultiplier` | スパイクエントリーのボリューム乗数。 | 1.0 |
| `EmergencyExit` | true の場合、すべてのポジションを即座にクローズします。 | 偽 |

## 注意事項

- StockSharp ポートは、MetaTrader によるチケットごとのマイクロ管理ではなく、クリーンな高レベルの API に重点を置いています。すべて
資金管理ロジックは、`Volume` および ATR ベースのレベルを通じて実装されます。
- 元の EA には、残高と証拠金のチェックがいくつかありました。これらは、`DailyProfitTarget`、`MaxNetPositions` で近似されます。
および ATR のサイズ変更パラメータにより、MT4 アカウントを直接呼び出さなくても動作の調整が維持されます。
- この戦略では平滑化された平均が使用されるため、取引を評価する前に十分なウォームアップ期間があることを確認してください。
