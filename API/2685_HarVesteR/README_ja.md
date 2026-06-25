# HarVesteR戦略
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

HarVesteR戦略は、MACDのモメンタムと2本の単純移動平均、およびオプションのADXトレンド強度フィルターを組み合わせています。
価格が移動平均に沿って推移しながらMACDが最近ゼロラインを越えた状況を探し、レンジからのブレイクアウトの可能性を示します。
ストップはスイング高値または安値に設置し、固定の報酬倍率でポジションの半分を決済し、残りは高速移動平均によるブレイクイーブン決済で保護します。

## 詳細

- **エントリー条件**:
  - ロング: `MACD > 0 && MACD history contains negative value && Close < SlowSMA && Close + Indentation > FastSMA && Close + Indentation > SlowSMA && ADX ≥ AdxBuyLevel (if enabled)`
  - ショート: `MACD < 0 && MACD history contains positive value && Close > SlowSMA && Close - Indentation < FastSMA && Close - Indentation < SlowSMA && ADX ≥ AdxSellLevel (if enabled)`
- **ストップロス**: `StopLookback`本の完成したローソク足における直近のスイング安値/高値。
- **部分決済**: 価格がエントリーとストップの距離の`HalfCloseRatio`倍動いた時点でポジションの半分を決済し、その後ストップをブレイクイーブンに移動します。
- **最終決済**:
  - ロング: ストップがブレイクイーブンに達した後、価格が`FastSMA + Indentation`を下回った場合に残りを決済します。
  - ショート: ストップがブレイクイーブンに達した後、価格が`FastSMA + Indentation`を上回った場合に残りを決済します。
- **ロング/ショート**: 両方向に対応。
- **フィルター**: オプションのADXトレンド強度フィルター。`UseAdxFilter`を`false`に設定すると無効化されます。
- **ポジション管理**: 反対シグナルのボリュームと現在のポジションを合算してポジションを反転します。

## パラメーター

| 名前 | デフォルト | 説明 |
|------|-----------|------|
| `MacdFast` | 12 | MACD差分ラインの高速EMA期間。 |
| `MacdSlow` | 24 | MACD差分ラインの低速EMA期間。 |
| `MacdSignal` | 9 | MACDスムージングのシグナルEMA期間。 |
| `MacdLookback` | 6 | MACDの符号変化を確認する直近完成ローソク足の数。 |
| `SmaFastLength` | 50 | 高速単純移動平均の期間。 |
| `SmaSlowLength` | 100 | 低速単純移動平均の期間。 |
| `MinIndentation` | 10 | エントリーまたは決済前に移動平均の周囲に適用するpips単位のオフセット。 |
| `StopLookback` | 6 | 初期ストップレベルを設定するためのスイング高値/安値のルックバック。 |
| `UseAdxFilter` | false | 両方向のADX強度フィルターを有効にします。 |
| `AdxBuyLevel` | 50 | フィルター有効時にロングエントリーを許可するために必要な最小ADXレベル。 |
| `AdxSellLevel` | 50 | フィルター有効時にショートエントリーを許可するために必要な最小ADXレベル。 |
| `AdxPeriod` | 14 | ADX計算に使用する期間。 |
| `HalfCloseRatio` | 2 | 部分利確前にエントリーからストップまでの距離に適用する乗数。 |
| `Volume` | 1 | 新規エントリーの注文量（反対ポジションとの相殺を含む）。 |
| `CandleType` | 1 hour | ローソク足とインジケーターの構築に使用するメインの時間軸。 |

## 注記

- `MinIndentation`は、銘柄のティックサイズを使用して価格距離に変換されます。3桁または5桁の小数で表示される銘柄は、pip単位に近似するために10倍の調整が行われます。
- `UseAdxFilter`が無効の場合、戦略はADX値を確認せずに両方向のシグナルを受け入れます。
- 部分利確とブレイクイーブン決済は、新規取引が許可されない場合でも未決済ポジションを保護するために、完成した各ローソク足で実行されます。
