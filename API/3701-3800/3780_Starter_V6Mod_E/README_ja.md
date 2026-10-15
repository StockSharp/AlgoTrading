# スターター V6 モッド E
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md)

**Starter V6 Mod E** は、MetaTrader 4 Expert Advisor `Starter_v6mod_e_www_forex-instruments_info.mq4` の高レベルの StockSharp 変換です。このポートは、ラゲール発振器の極値、デュアル EMA 運動量確認、CCI フィルタリング、および EMA 角度ゲートの元の組み合わせを維持しながら、実行を StockSharp のイベント駆動型アーキテクチャに適応させます。

## 取引ロジック

- **トレンド ゲート:** 34 期間の EMA の傾きが、構成可能な開始/終了シフト間で測定されます。傾きはピップ単位で表されます。正の傾きのみロング取引が許可され、負の傾きのみショートが許可され、フラットな読み取り値は新規エントリーをブロックします。
- **ラゲールの極値:** 手作りのラゲール RSI (デフォルトではガンマ = 0.7) は、売られすぎ/買われすぎの状態を 0 ～ 1 のスケールで追跡します。ロングの場合は、現在値と以前の値の両方が `Laguerre Oversold` レベル未満に留まる必要があり、ショートの場合は両方の値が `Laguerre Overbought` を超える必要があります。
- **EMA モメンタム フィルター:** 120 および 40 期間の EMA (中央価格) は、元の MA フィルターを反映して、ロングの場合は両方とも上昇し、ショートの場合は両方とも下落する必要があります。
- **CCI 確認:** 14 期間の CCI は、ロングの場合は `-CCI Threshold` より下、ショートの場合は `+CCI Threshold` より上でなければならず、MQL からの `Alpha` フィルターを複製します。
- **金曜日の安全性:** 新しい取引は `Friday Block Hour` 以降ブロックされ、残りのポジションは `Friday Exit Hour` に達すると清算されます。

## リスク管理

- 構成可能なストップロス、テイクプロフィット、およびトレーリングストップの距離 (ピップ単位) は、エキスパートの資金管理ブロックをエミュレートします。
- トレーリングストップはエントリー後に最も有利な価格に従い、リトレースメントが設定された距離を超えたときに取引を終了します。
- 手動ポジション決済は `SellMarket`/`BuyMarket` を通じて実行され、高レベルの API コンプライアンスが保証されます。

## パラメーター

| パラメータ | 説明 |
|-----------|-------------|
| `Volume` | 各市場参入の注文量。 |
| `StopLossPips` | ピップ単位の保護停止距離。 |
| `TakeProfitPips` | 利益目標（pips）。 |
| `TrailingStopPips` | トレーリングストップの距離 (ピップ単位) (0 はトレーリングを無効にします)。 |
| `SlowEmaPeriod` | PRICE_MEDIAN に計算された遅い EMA の期間。 |
| `FastEmaPeriod` | PRICE_MEDIAN で計算された高速 EMA の期間。 |
| `AngleEmaPeriod` | EMA 周期が角度検出器に使用されます。 |
| `AngleStartShift` / `AngleEndShift` | EMA の傾きを計算するために使用されるバー シフト。 |
| `AngleThreshold` | 取引を可能にするために必要な最小勾配 (ピップ単位)。 |
| `CciPeriod` / `CciThreshold` | CCI フィルターの期間と絶対しきい値。 |
| `LaguerreGamma` | ラゲール発振器のガンマパラメータ。 |
| `LaguerreOversold` / `LaguerreOverbought` | 0 ～ 1 ラゲール スケールのエントリしきい値。 |
| `CandleType` | ローソク足のデータ型 (デフォルトは 1 分)。 |
| `FridayBlockHour` / `FridayExitHour` | 金曜日のリスク制限を制御する時間 (現地の機器時間)。 |

## 変換メモ

- ラゲール発振器は、元の再帰式から直接実装され、0 ～ 1 の出力範囲とガンマ スムージングを維持します。
- EMA の傾きは、過去の EMA ポイント間の pip 正規化された差分を計算することにより、MQL 角度ヘルパーを置き換えます。
- 株式カットオフやグリッド スタッキングなどの資金管理機能は、変換される MT4 バリアントによってデフォルトで無効になっており、StockSharp が明示的なポートフォリオ管理を奨励しているため、意図的に省略されています。
- 注文は `BuyMarket`/`SellMarket` を通じて送信され、`OnNewMyTrade` を利用してトレーリング ロジックの約定価格を追跡します。
